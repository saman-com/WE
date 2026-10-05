using EvidenceService.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace EvidenceService.Infrastructure.Data;

public sealed class EvidenceDbContext(
    DbContextOptions<EvidenceDbContext> options,
    ITenantContext tenantContext) : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<EducationalEvidence> Evidence => Set<EducationalEvidence>();
    public DbSet<EvidenceMicroSkillMark> MicroSkillMarks => Set<EvidenceMicroSkillMark>();

    public override int SaveChanges()
    {
        RejectMutationsOfApprovedEvidence();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RejectMutationsOfApprovedEvidence();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EducationalEvidence>(entity =>
        {
            entity.ToTable("educational_evidence");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.ApprovedByTeacherUserId).HasColumnName("approved_by_teacher_user_id").IsRequired();
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.AssessmentId);
            entity.HasIndex(e => e.StudentUserId);
            entity.HasIndex(e => e.SubmissionId).IsUnique();
            entity.HasMany(e => e.MicroSkillMarks).WithOne(e => e.Evidence)
                .HasForeignKey(e => e.EvidenceId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenceMicroSkillMark>(entity =>
        {
            entity.ToTable("evidence_micro_skill_marks");
            entity.HasKey(e => new { e.EvidenceId, e.MicroSkillId });
            entity.ConfigureTenantId();
            entity.Property(e => e.EvidenceId).HasColumnName("evidence_id");
            entity.Property(e => e.MicroSkillId).HasColumnName("micro_skill_id");
            entity.Property(e => e.Mark).HasColumnName("mark");
            entity.Property(e => e.Feedback).HasColumnName("feedback").IsRequired();
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }

    private void RejectMutationsOfApprovedEvidence()
    {
        foreach (var entry in ChangeTracker.Entries<EducationalEvidence>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Approved evidence is immutable.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<EvidenceMicroSkillMark>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Approved evidence is immutable.");
            }
        }
    }
}
