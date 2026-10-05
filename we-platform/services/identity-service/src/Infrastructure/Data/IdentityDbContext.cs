using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace IdentityService.Infrastructure.Data;

public sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    ITenantContext tenantContext) : IdentityDbContext<ApplicationUser>(options)
{
    private readonly ITenantContext _tenantContext = tenantContext;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .IsRequired();
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.FederationId).HasColumnName("federation_id");
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.ApplyTenantOnSave(_tenantContext);
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.ApplyTenantOnSave(_tenantContext);
        return base.SaveChangesAsync(cancellationToken);
    }
}
