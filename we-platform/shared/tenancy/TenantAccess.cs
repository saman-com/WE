using Microsoft.AspNetCore.Http;

namespace WePlatform.Tenancy;

public static class TenantAccess
{
    public static IResult? ValidateEntityAccess(ITenantContext tenantContext, ITenantEntity? entity)
    {
        if (entity is null)
        {
            return null;
        }

        if (!tenantContext.HasTenant)
        {
            return Results.Forbid();
        }

        return entity.TenantId == tenantContext.TenantId
            ? null
            : Results.Forbid();
    }
}
