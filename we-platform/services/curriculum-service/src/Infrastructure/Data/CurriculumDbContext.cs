using CurriculumService.Domain;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace CurriculumService.Infrastructure.Data;

public sealed class CurriculumDbContext(
    DbContextOptions<CurriculumDbContext> options,
    ITenantContext tenantContext) : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<Curriculum> Curricula => Set<Curriculum>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<LearningObjective> LearningObjectives => Set<LearningObjective>();
    public DbSet<MicroSkill> MicroSkills => Set<MicroSkill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Curriculum>(entity =>
        {
            entity.ToTable("curricula");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.OrganisationId);
            entity.HasMany(e => e.Subjects).WithOne(e => e.Curriculum).HasForeignKey(e => e.CurriculumId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.ToTable("subjects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.CurriculumId).HasColumnName("curriculum_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Code).HasColumnName("code").IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(e => new { e.CurriculumId, e.Code }).IsUnique();
            entity.HasMany(e => e.Units).WithOne(e => e.Subject).HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.ToTable("units");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.SubjectId).HasColumnName("subject_id").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(e => e.SubjectId);
            entity.HasMany(e => e.Topics).WithOne(e => e.Unit).HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.LearningObjectives).WithOne(e => e.Unit).HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LearningObjective>(entity =>
        {
            entity.ToTable("learning_objectives");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.UnitId).HasColumnName("unit_id").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(e => e.UnitId);
            entity.HasMany(e => e.MicroSkills).WithOne(e => e.LearningObjective)
                .HasForeignKey(e => e.LearningObjectiveId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MicroSkill>(entity =>
        {
            entity.ToTable("micro_skills");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.LearningObjectiveId).HasColumnName("learning_objective_id").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(e => e.LearningObjectiveId);
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.ToTable("topics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.UnitId).HasColumnName("unit_id").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(e => e.UnitId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
