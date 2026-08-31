using Microsoft.EntityFrameworkCore;
using OrganisationService.Domain;
using WePlatform.Tenancy;

namespace OrganisationService.Infrastructure.Data;

public sealed class OrganisationDbContext(
    DbContextOptions<OrganisationDbContext> options,
    ITenantContext tenantContext) : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<YearLevel> YearLevels => Set<YearLevel>();
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<ClassTeacher> ClassTeachers => Set<ClassTeacher>();
    public DbSet<ClassEnrollment> ClassEnrollments => Set<ClassEnrollment>();
    public DbSet<ParentStudentLink> ParentStudentLinks => Set<ParentStudentLink>();
    public DbSet<OrganisationLeader> OrganisationLeaders => Set<OrganisationLeader>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organisation>(entity =>
        {
            entity.ToTable("organisations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Code).HasColumnName("code").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasMany(e => e.YearLevels).WithOne(e => e.Organisation).HasForeignKey(e => e.OrganisationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Classes).WithOne(e => e.Organisation).HasForeignKey(e => e.OrganisationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Leaders).WithOne(e => e.Organisation).HasForeignKey(e => e.OrganisationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<YearLevel>(entity =>
        {
            entity.ToTable("year_levels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(e => new { e.OrganisationId, e.Name }).IsUnique();
            entity.HasMany(e => e.Classes).WithOne(e => e.YearLevel).HasForeignKey(e => e.YearLevelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SchoolClass>(entity =>
        {
            entity.ToTable("classes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.YearLevelId).HasColumnName("year_level_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Code).HasColumnName("code").IsRequired();
            entity.HasIndex(e => new { e.OrganisationId, e.Code }).IsUnique();
            entity.HasMany(e => e.Teachers).WithOne(e => e.Class).HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Enrollments).WithOne(e => e.Class).HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClassTeacher>(entity =>
        {
            entity.ToTable("class_teachers");
            entity.HasKey(e => new { e.ClassId, e.TeacherUserId });
            entity.ConfigureTenantId();
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.TeacherUserId).HasColumnName("teacher_user_id");
        });

        modelBuilder.Entity<ClassEnrollment>(entity =>
        {
            entity.ToTable("class_enrollments");
            entity.HasKey(e => new { e.ClassId, e.StudentUserId });
            entity.ConfigureTenantId();
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id");
        });

        modelBuilder.Entity<ParentStudentLink>(entity =>
        {
            entity.ToTable("parent_student_links");
            entity.HasKey(e => new { e.ParentUserId, e.StudentUserId });
            entity.ConfigureTenantId();
            entity.Property(e => e.ParentUserId).HasColumnName("parent_user_id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id");
            entity.Property(e => e.LinkedAt).HasColumnName("linked_at");
        });

        modelBuilder.Entity<OrganisationLeader>(entity =>
        {
            entity.ToTable("organisation_leaders");
            entity.HasKey(e => new { e.OrganisationId, e.LeaderUserId });
            entity.ConfigureTenantId();
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.LeaderUserId).HasColumnName("leader_user_id");
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
        });

        base.OnModelCreating(modelBuilder);
    }
}
