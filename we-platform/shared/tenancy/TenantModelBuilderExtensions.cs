using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace WePlatform.Tenancy;

/// <summary>
/// Legacy helpers. Prefer filters applied by <see cref="TenantAwareDbContext"/> which bind
/// <see cref="TenantAwareDbContext.CurrentTenantId"/> so EF re-evaluates per context instance.
/// </summary>
public static class TenantModelBuilderExtensions
{
    public static void ApplyTenantQueryFilters(this ModelBuilder modelBuilder, ITenantContext tenantContext)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var method = typeof(TenantModelBuilderExtensions)
                .GetMethod(nameof(ApplyTenantQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, [modelBuilder, tenantContext]);
        }
    }

    private static void ApplyTenantQueryFilter<TEntity>(
        ModelBuilder modelBuilder,
        ITenantContext tenantContext)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(CreateTenantFilter<TEntity>(tenantContext));
    }

    private static Expression<Func<TEntity, bool>> CreateTenantFilter<TEntity>(ITenantContext tenantContext)
        where TEntity : class, ITenantEntity
    {
        // Fail-closed. Prefer TenantAwareDbContext.CurrentTenantId filters for correct EF binding.
        return entity => tenantContext.TenantId.HasValue && entity.TenantId == tenantContext.TenantId;
    }
}
