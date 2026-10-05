using LearningGapService.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace LearningGapService.Infrastructure.Data;

public sealed class GapDbContext(
    DbContextOptions<GapDbContext> options,
    ITenantContext tenantContext) : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<LearningGap> Gaps => Set<LearningGap>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LearningGap>(entity =>
        {
            entity.ToTable("learning_gaps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.EvidenceId).HasColumnName("evidence_id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.MicroSkillId).HasColumnName("micro_skill_id");
            entity.Property(e => e.LearningObjectiveId).HasColumnName("learning_objective_id");
            entity.Property(e => e.ExpectedMastery).HasColumnName("expected_mastery").IsRequired();
            entity.Property(e => e.ActualMastery).HasColumnName("actual_mastery").IsRequired();
            entity.Property(e => e.Mark).HasColumnName("mark");
            entity.Property(e => e.Severity).HasColumnName("severity").IsRequired();
            entity.Property(e => e.Urgency).HasColumnName("urgency").IsRequired();
            entity.Property(e => e.Explanation).HasColumnName("explanation").IsRequired();
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
