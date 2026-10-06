using EdwIngestService.Application;
using EdwIngestService.Domain;
using EdwIngestService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace EdwIngestService.Infrastructure;

public sealed class EdwIngestProcessor(EdwDbContext db, ITenantContext tenantContext) : IEdwIngestProcessor
{
    public async Task ProcessEvidenceCreatedAsync(
        EvidenceCreated evidence,
        CancellationToken cancellationToken = default)
    {
        tenantContext.SetTenant(evidence.OrganisationId);

        if (await db.EvidenceFacts.AnyAsync(f => f.EventId == evidence.EventId, cancellationToken))
        {
            return;
        }

        var ingestedAt = DateTimeOffset.UtcNow;
        var timeKey = await EnsureTimeDimensionAsync(evidence.ApprovedAt, cancellationToken);

        db.EvidenceFacts.Add(new EvidenceFact
        {
            TenantId = evidence.OrganisationId,
            EventId = evidence.EventId,
            EvidenceId = evidence.EvidenceId,
            OrganisationId = evidence.OrganisationId,
            AssessmentId = evidence.AssessmentId,
            SubmissionId = evidence.SubmissionId,
            StudentUserId = evidence.StudentUserId,
            ClassId = evidence.ClassId,
            ApprovedByTeacherUserId = evidence.ApprovedByTeacherUserId,
            ApprovedAt = evidence.ApprovedAt,
            TimeKey = timeKey,
            MicroSkillCount = evidence.MicroSkillMarks.Count,
            IngestedAt = ingestedAt
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessAssessmentApprovedAsync(
        AssessmentApproved assessment,
        CancellationToken cancellationToken = default)
    {
        tenantContext.SetTenant(assessment.OrganisationId);

        if (await db.AssessmentFacts.AnyAsync(f => f.EventId == assessment.EventId, cancellationToken))
        {
            return;
        }

        var ingestedAt = DateTimeOffset.UtcNow;
        var timeKey = await EnsureTimeDimensionAsync(assessment.ApprovedAt, cancellationToken);

        db.AssessmentFacts.Add(new AssessmentFact
        {
            TenantId = assessment.OrganisationId,
            EventId = assessment.EventId,
            AssessmentId = assessment.AssessmentId,
            OrganisationId = assessment.OrganisationId,
            SubmissionId = assessment.SubmissionId,
            EvidenceId = assessment.EvidenceId,
            StudentUserId = assessment.StudentUserId,
            ApprovedByTeacherUserId = assessment.ApprovedByTeacherUserId,
            ApprovedAt = assessment.ApprovedAt,
            TimeKey = timeKey,
            MicroSkillCount = assessment.MicroSkillResults.Count,
            IngestedAt = ingestedAt
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessInterventionCreatedAsync(
        InterventionCreated intervention,
        CancellationToken cancellationToken = default)
    {
        tenantContext.SetTenant(intervention.OrganisationId);

        var existing = await db.InterventionFacts
            .FirstOrDefaultAsync(f => f.InterventionId == intervention.InterventionId, cancellationToken);
        if (existing is not null)
        {
            if (existing.EventId == intervention.EventId || StatusRank(intervention.Status) < StatusRank(existing.Status))
            {
                return;
            }

            existing.Status = intervention.Status;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (await db.InterventionFacts.AnyAsync(f => f.EventId == intervention.EventId, cancellationToken))
        {
            return;
        }

        var ingestedAt = DateTimeOffset.UtcNow;
        var timeKey = await EnsureTimeDimensionAsync(intervention.CreatedAt, cancellationToken);

        db.InterventionFacts.Add(new InterventionFact
        {
            TenantId = intervention.OrganisationId,
            EventId = intervention.EventId,
            InterventionId = intervention.InterventionId,
            OrganisationId = intervention.OrganisationId,
            StudentUserId = intervention.StudentUserId,
            LearningGapId = intervention.LearningGapId,
            AssignedTeacherUserId = intervention.AssignedTeacherUserId,
            Status = intervention.Status,
            CreatedAt = intervention.CreatedAt,
            TimeKey = timeKey,
            IngestedAt = ingestedAt
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessEvidenceBatchAsync(
        IReadOnlyList<EvidenceCreated> events,
        CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            return;
        }

        var ingestedAt = DateTimeOffset.UtcNow;
        var existingSet = new HashSet<Guid>();
        foreach (var organisationId in events.Select(e => e.OrganisationId).Distinct())
        {
            tenantContext.SetTenant(organisationId);
            var existingEventIds = await db.EvidenceFacts
                .Where(f => events.Select(e => e.EventId).Contains(f.EventId))
                .Select(f => f.EventId)
                .ToListAsync(cancellationToken);
            foreach (var id in existingEventIds)
            {
                existingSet.Add(id);
            }
        }

        foreach (var evidence in events)
        {
            if (existingSet.Contains(evidence.EventId))
            {
                continue;
            }

            tenantContext.SetTenant(evidence.OrganisationId);
            var timeKey = await EnsureTimeDimensionAsync(evidence.ApprovedAt, cancellationToken);
            db.EvidenceFacts.Add(new EvidenceFact
            {
                TenantId = evidence.OrganisationId,
                EventId = evidence.EventId,
                EvidenceId = evidence.EvidenceId,
                OrganisationId = evidence.OrganisationId,
                AssessmentId = evidence.AssessmentId,
                SubmissionId = evidence.SubmissionId,
                StudentUserId = evidence.StudentUserId,
                ClassId = evidence.ClassId,
                ApprovedByTeacherUserId = evidence.ApprovedByTeacherUserId,
                ApprovedAt = evidence.ApprovedAt,
                TimeKey = timeKey,
                MicroSkillCount = evidence.MicroSkillMarks.Count,
                IngestedAt = ingestedAt
            });
            existingSet.Add(evidence.EventId);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static int StatusRank(string status) => status switch
    {
        "Active" => 1,
        "Completed" or "Closed" => 2,
        _ => 0
    };

    private async Task<int> EnsureTimeDimensionAsync(
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var date = DateOnly.FromDateTime(timestamp.UtcDateTime);
        var dateKey = date.Year * 10_000 + date.Month * 100 + date.Day;

        // Shared calendar dimension is stored under DefaultTenant, not the school tenant.
        var exists = await db.DimTimes
            .IgnoreQueryFilters()
            .AnyAsync(d => d.DateKey == dateKey, cancellationToken);
        if (!exists)
        {
            db.DimTimes.Add(new DimTime
            {
                TenantId = DefaultTenant.Id,
                DateKey = dateKey,
                CalendarDate = date,
                Year = date.Year,
                Month = date.Month,
                Day = date.Day
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return dateKey;
    }
}
