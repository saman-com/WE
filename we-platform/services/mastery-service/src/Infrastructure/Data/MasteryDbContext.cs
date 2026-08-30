using Microsoft.EntityFrameworkCore;
using MasteryService.Domain;

namespace MasteryService.Infrastructure.Data;

public sealed class MasteryDbContext(DbContextOptions<MasteryDbContext> options) : DbContext(options)
{
    public DbSet<MasteryRecord> Records => Set<MasteryRecord>();
    public DbSet<MasteryEvidenceMark> EvidenceMarks => Set<MasteryEvidenceMark>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MasteryEvidenceMark>(entity =>
        {
            entity.ToTable("mastery_evidence_marks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.MicroSkillId).HasColumnName("micro_skill_id");
            entity.Property(e => e.EvidenceId).HasColumnName("evidence_id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.Mark).HasColumnName("mark");
            entity.Property(e => e.Weight).HasColumnName("weight");
            entity.Property(e => e.RecordedAt).HasColumnName("recorded_at");
            entity.HasIndex(e => new { e.EvidenceId, e.MicroSkillId }).IsUnique();
            entity.HasIndex(e => new { e.StudentUserId, e.MicroSkillId });
        });

        modelBuilder.Entity<MasteryRecord>(entity =>
        {
            entity.ToTable("mastery_records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.MicroSkillId).HasColumnName("micro_skill_id");
            entity.Property(e => e.MasteryLevel).HasColumnName("mastery_level").IsRequired();
            entity.Property(e => e.WeightedAverage).HasColumnName("weighted_average");
            entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
            entity.Property(e => e.EvidenceCount).HasColumnName("evidence_count");
            entity.Property(e => e.Explanation).HasColumnName("explanation").IsRequired();
            entity.Property(e => e.CalculatedAt).HasColumnName("calculated_at");
            entity.HasIndex(e => e.StudentUserId);
            entity.HasIndex(e => new { e.StudentUserId, e.MicroSkillId }).IsUnique();
        });
    }
}
