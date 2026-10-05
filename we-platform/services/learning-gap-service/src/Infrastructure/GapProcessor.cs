using LearningGapService.Application;
using LearningGapService.Domain;
using LearningGapService.Infrastructure.Data;
using LearningGapService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace LearningGapService.Infrastructure;

public sealed class GapProcessor(
    GapDbContext db,
    IGapCalculationEngine calculationEngine,
    ITenantContext tenantContext) : IGapProcessor
{
    public async Task ProcessEvidenceCreatedAsync(
        EvidenceCreated evidence,
        CancellationToken cancellationToken = default)
    {
        tenantContext.SetTenant(evidence.OrganisationId);

        var diagnostics = evidence.MicroSkillMarks
            .Select(mark => DiagnosticClassifier.ToDiagnosticInput(evidence, mark))
            .ToList();
        var calculated = calculationEngine.CalculateFromDiagnostics(diagnostics);
        var now = DateTimeOffset.UtcNow;

        foreach (var gap in calculated)
        {
            var exists = await db.Gaps.AnyAsync(
                g => g.EvidenceId == gap.EvidenceId && g.MicroSkillId == gap.MicroSkillId,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            db.Gaps.Add(new LearningGap
            {
                Id = Guid.CreateVersion7(),
                TenantId = TenantBackfill.ResolveOrganisationTenant(new LearningGap
                {
                    OrganisationId = evidence.OrganisationId
                }),
                StudentUserId = evidence.StudentUserId,
                OrganisationId = evidence.OrganisationId,
                EvidenceId = gap.EvidenceId,
                AssessmentId = gap.AssessmentId,
                MicroSkillId = gap.MicroSkillId,
                LearningObjectiveId = null,
                ExpectedMastery = gap.ExpectedMastery,
                ActualMastery = gap.ActualMastery,
                Mark = gap.Mark,
                Severity = gap.Severity,
                Urgency = gap.Urgency,
                Explanation = gap.Explanation,
                CreatedAt = now
            });
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (UniqueConstraint.IsViolation(ex))
        {
            db.ChangeTracker.Clear();
        }
    }
}
