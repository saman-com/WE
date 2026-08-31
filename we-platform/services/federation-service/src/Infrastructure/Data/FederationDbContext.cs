using FederationService.Domain;
using Microsoft.EntityFrameworkCore;

namespace FederationService.Infrastructure.Data;

public sealed class FederationDbContext(DbContextOptions<FederationDbContext> options) : DbContext(options)
{
    public DbSet<FederationSchool> FederationSchools => Set<FederationSchool>();
    public DbSet<SchoolAdminAssignment> SchoolAdminAssignments => Set<SchoolAdminAssignment>();
    public DbSet<FederationPolicy> FederationPolicies => Set<FederationPolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FederationSchool>(entity =>
        {
            entity.ToTable("federation_schools");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.Property(e => e.FederationId).HasColumnName("federation_id");
            entity.HasIndex(e => new { e.FederationId, e.Code }).IsUnique();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Code).HasColumnName("code").IsRequired();
            entity.Property(e => e.EnrollmentCount).HasColumnName("enrollment_count");
            entity.Property(e => e.AverageProgressPercent).HasColumnName("average_progress_percent");
            entity.Property(e => e.HasDefaultConfiguration).HasColumnName("has_default_configuration");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<SchoolAdminAssignment>(entity =>
        {
            entity.ToTable("school_admin_assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FederationId).HasColumnName("federation_id");
            entity.Property(e => e.SchoolTenantId).HasColumnName("school_tenant_id");
            entity.HasIndex(e => new { e.SchoolTenantId, e.UserId }).IsUnique();
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
            entity.HasOne(e => e.School)
                .WithMany(s => s.AdminAssignments)
                .HasForeignKey(e => e.SchoolTenantId)
                .HasPrincipalKey(s => s.TenantId);
        });

        modelBuilder.Entity<FederationPolicy>(entity =>
        {
            entity.ToTable("federation_policies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FederationId).HasColumnName("federation_id");
            entity.HasIndex(e => new { e.FederationId, e.PolicyKey }).IsUnique();
            entity.Property(e => e.PolicyKey).HasColumnName("policy_key").IsRequired();
            entity.Property(e => e.PolicyValue).HasColumnName("policy_value").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        base.OnModelCreating(modelBuilder);
    }
}
