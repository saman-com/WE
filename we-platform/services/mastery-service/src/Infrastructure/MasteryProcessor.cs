using MasteryService.Application;
using MasteryService.Domain;
using MasteryService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace MasteryService.Infrastructure;

public sealed class MasteryProcessor(
    MasteryDbContext db,
    IMasteryCalculationEngine calculationEngine,
    MasteryThresholds thresholds) : IMasteryProcessor
{
    public async Task ProcessEvidenceCreatedAsync(
        EvidenceCreated evidence,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var result in evidence.MicroSkillMarks)
        {
            var markExists = await db.EvidenceMarks.AnyAsync(
                m => m.EvidenceId == evidence.EvidenceId && m.MicroSkillId == result.MicroSkillId,
                cancellationToken);
            if (markExists)
            {
                continue;
            }

            db.EvidenceMarks.Add(new MasteryEvidenceMark
            {
                Id = Guid.CreateVersion7(),
                TenantId = TenantBackfill.ResolveOrganisationTenant(new MasteryEvidenceMark
                {
                    OrganisationId = evidence.OrganisationId
                }),
                StudentUserId = evidence.StudentUserId,
                OrganisationId = evidence.OrganisationId,
                MicroSkillId = result.MicroSkillId,
                EvidenceId = evidence.EvidenceId,
                AssessmentId = evidence.AssessmentId,
                Mark = result.Mark,
                Weight = thresholds.DefaultEvidenceWeight,
                RecordedAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var microSkillIds = evidence.MicroSkillMarks
            .Select(mark => mark.MicroSkillId)
            .Distinct()
            .ToList();

        foreach (var microSkillId in microSkillIds)
        {
            await RecalculateMasteryAsync(
                evidence.StudentUserId,
                evidence.OrganisationId,
                microSkillId,
                now,
                cancellationToken);
        }
    }

    private async Task RecalculateMasteryAsync(
        string studentUserId,
        Guid organisationId,
        Guid microSkillId,
        DateTimeOffset calculatedAt,
        CancellationToken cancellationToken)
    {
        var marks = await db.EvidenceMarks
            .Where(m => m.StudentUserId == studentUserId && m.MicroSkillId == microSkillId)
            .OrderBy(m => m.RecordedAt)
            .ToListAsync(cancellationToken);

        var inputs = marks
            .Select(m => new EvidenceMarkInput(m.Mark, m.Weight))
            .ToList();
        var calculated = calculationEngine.Calculate(inputs);

        var existing = await db.Records.FirstOrDefaultAsync(
            r => r.StudentUserId == studentUserId && r.MicroSkillId == microSkillId,
            cancellationToken);

        if (existing is null)
        {
            db.Records.Add(new MasteryRecord
            {
                Id = Guid.CreateVersion7(),
                TenantId = TenantBackfill.ResolveOrganisationTenant(new MasteryRecord
                {
                    OrganisationId = organisationId
                }),
                StudentUserId = studentUserId,
                OrganisationId = organisationId,
                MicroSkillId = microSkillId,
                MasteryLevel = calculated.MasteryLevel,
                WeightedAverage = calculated.WeightedAverage,
                ConfidenceScore = calculated.ConfidenceScore,
                EvidenceCount = calculated.EvidenceCount,
                Explanation = calculated.Explanation,
                CalculatedAt = calculatedAt
            });
        }
        else
        {
            existing.MasteryLevel = calculated.MasteryLevel;
            existing.WeightedAverage = calculated.WeightedAverage;
            existing.ConfidenceScore = calculated.ConfidenceScore;
            existing.EvidenceCount = calculated.EvidenceCount;
            existing.Explanation = calculated.Explanation;
            existing.CalculatedAt = calculatedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
