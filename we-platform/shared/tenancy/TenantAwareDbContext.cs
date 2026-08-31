using Microsoft.EntityFrameworkCore;

namespace WePlatform.Tenancy;

public abstract class TenantAwareDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    protected TenantAwareDbContext(DbContextOptions options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyTenantQueryFilters(_tenantContext);
        base.OnModelCreating(modelBuilder);
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
