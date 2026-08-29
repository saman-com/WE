using Microsoft.EntityFrameworkCore;
using DiagnosticService.Domain;

namespace DiagnosticService.Infrastructure.Data;

public sealed class DiagnosticDbContext(DbContextOptions<DiagnosticDbContext> options) : DbContext(options)
{
    public DbSet<MicroSkillDiagnostic> Diagnostics => Set<MicroSkillDiagnostic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MicroSkillDiagnostic>(entity =>
        {
            entity.ToTable("micro_skill_diagnostics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
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
    }
}
