using InterventionService.Domain;
using Microsoft.EntityFrameworkCore;

namespace InterventionService.Infrastructure.Data;

public sealed class InterventionDbContext(DbContextOptions<InterventionDbContext> options) : DbContext(options)
{
    public DbSet<Intervention> Interventions => Set<Intervention>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Intervention>(entity =>
        {
            entity.ToTable("interventions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.LearningGapId).HasColumnName("learning_gap_id");
            entity.Property(e => e.AssignedTeacherUserId).HasColumnName("assigned_teacher_user_id").IsRequired();
            entity.Property(e => e.PlannedActions).HasColumnName("planned_actions").IsRequired();
            entity.Property(e => e.Notes).HasColumnName("notes").IsRequired();
            entity.Property(e => e.Outcome).HasColumnName("outcome");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.PlannedStartAt).HasColumnName("planned_start_at");
            entity.Property(e => e.PlannedEndAt).HasColumnName("planned_end_at");
            entity.Property(e => e.ReviewAt).HasColumnName("review_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.StudentUserId);
            entity.HasIndex(e => e.LearningGapId);
            entity.HasIndex(e => e.AssignedTeacherUserId);
        });
    }
}
