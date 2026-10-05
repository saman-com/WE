using DiagnosticService.Application;
using DiagnosticService.Domain;
using DiagnosticService.Infrastructure.Data;
using DiagnosticService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace DiagnosticService.Infrastructure;

public sealed class DiagnosticProcessor(
    DiagnosticDbContext db,
    IDiagnosticAnalysisEngine analysisEngine,
    ITenantContext tenantContext) : IDiagnosticProcessor
{
    public async Task ProcessEvidenceCreatedAsync(
        EvidenceCreated evidence,
        CancellationToken cancellationToken = default)
    {
        tenantContext.SetTenant(evidence.OrganisationId);

        var analyzed = analysisEngine.Analyze(evidence);
        var now = DateTimeOffset.UtcNow;

        foreach (var item in analyzed)
        {
            var exists = await db.Diagnostics.AnyAsync(
                d => d.EvidenceId == item.EvidenceId && d.MicroSkillId == item.MicroSkillId,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            db.Diagnostics.Add(new MicroSkillDiagnostic
            {
                Id = Guid.CreateVersion7(),
                TenantId = evidence.OrganisationId,
                StudentUserId = evidence.StudentUserId,
                OrganisationId = evidence.OrganisationId,
                EvidenceId = item.EvidenceId,
                AssessmentId = item.AssessmentId,
                MicroSkillId = item.MicroSkillId,
                Status = item.Status,
                Mark = item.Mark,
                Reason = item.Reason,
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
