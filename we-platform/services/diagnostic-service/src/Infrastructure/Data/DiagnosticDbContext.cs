using DiagnosticService.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace DiagnosticService.Infrastructure.Data;

public sealed class DiagnosticDbContext(
    DbContextOptions<DiagnosticDbContext> options,
    ITenantContext tenantContext) : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<MicroSkillDiagnostic> Diagnostics => Set<MicroSkillDiagnostic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MicroSkillDiagnostic>(entity =>
        {
            entity.ToTable("micro_skill_diagnostics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.EvidenceId).HasColumnName("evidence_id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.MicroSkillId).HasColumnName("micro_skill_id");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.Mark).HasColumnName("mark");
            entity.Property(e => e.Reason).HasColumnName("reason").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.StudentUserId);
            entity.HasIndex(e => new { e.EvidenceId, e.MicroSkillId }).IsUnique();
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }
}
