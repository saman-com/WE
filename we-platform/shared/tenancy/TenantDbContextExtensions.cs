using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace WePlatform.Tenancy;

public static class TenantDbContextExtensions
{
    public static void ApplyTenantOnSave(this DbContext dbContext, ITenantContext tenantContext)
    {
        if (!tenantContext.HasTenant)
        {
            return;
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = tenantContext.TenantId!.Value;
            }
            else if (entry.State == EntityState.Modified
                     && entry.Entity.TenantId != tenantContext.TenantId!.Value)
            {
                throw new InvalidOperationException("Cross-tenant entity modification is not permitted.");
            }
        }
    }

    public static async Task BackfillTenantIdsAsync<TEntity>(
        this DbContext dbContext,
        Func<TEntity, Guid> tenantIdResolver,
        CancellationToken cancellationToken = default)
        where TEntity : class, ITenantEntity
    {
        var entities = await dbContext.Set<TEntity>()
            .IgnoreQueryFilters()
            .Where(entity => entity.TenantId == Guid.Empty)
            .ToListAsync(cancellationToken);

        if (entities.Count == 0)
        {
            return;
        }

        foreach (var entity in entities)
        {
            entity.TenantId = tenantIdResolver(entity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
