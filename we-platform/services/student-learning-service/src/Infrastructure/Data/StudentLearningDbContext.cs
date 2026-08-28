using Microsoft.EntityFrameworkCore;
using StudentLearningService.Domain;

namespace StudentLearningService.Infrastructure.Data;

public sealed class StudentLearningDbContext(DbContextOptions<StudentLearningDbContext> options) : DbContext(options)
{
    public DbSet<StudentLearningProfile> Profiles => Set<StudentLearningProfile>();
    public DbSet<ProfileClassEnrollment> ProfileEnrollments => Set<ProfileClassEnrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentLearningProfile>(entity =>
        {
            entity.ToTable("student_learning_profiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.StudentUserId).IsUnique();
            entity.HasMany(e => e.Enrollments).WithOne(e => e.Profile).HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProfileClassEnrollment>(entity =>
        {
            entity.ToTable("profile_class_enrollments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.ClassName).HasColumnName("class_name").IsRequired();
            entity.Property(e => e.ClassCode).HasColumnName("class_code").IsRequired();
            entity.Property(e => e.EnrolledAt).HasColumnName("enrolled_at");
            entity.HasIndex(e => new { e.ProfileId, e.ClassId }).IsUnique();
        });
    }
}
