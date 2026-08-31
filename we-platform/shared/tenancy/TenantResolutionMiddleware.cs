using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace WePlatform.Tenancy;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly string[] ExemptPathPrefixes =
    [
        "/health"
    ];

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (IsExemptPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var tenantClaim = context.User.FindFirstValue(TenantClaimTypes.TenantId);
        if (!Guid.TryParse(tenantClaim, out var tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        tenantContext.SetTenant(tenantId);

        if (!ValidateRouteTenantScope(context, tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }

    private static bool IsExemptPath(PathString path) =>
        ExemptPathPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));

    private static bool ValidateRouteTenantScope(HttpContext context, Guid tenantId)
    {
        if (context.Request.RouteValues.TryGetValue("organisationId", out var routeOrgId)
            && Guid.TryParse(routeOrgId?.ToString(), out var routeOrgGuid)
            && routeOrgGuid != tenantId)
        {
            return false;
        }

        if (context.Request.RouteValues.TryGetValue("tenantId", out var routeTenantId)
            && Guid.TryParse(routeTenantId?.ToString(), out var routeTenantGuid)
            && routeTenantGuid != tenantId)
        {
            return false;
        }

        if (context.Request.Query.TryGetValue("organisationId", out var queryOrgId)
            && Guid.TryParse(queryOrgId.ToString(), out var queryOrgGuid)
            && queryOrgGuid != tenantId)
        {
            return false;
        }

        if (context.Request.Query.TryGetValue("tenantId", out var queryTenantId)
            && Guid.TryParse(queryTenantId.ToString(), out var queryTenantGuid)
            && queryTenantGuid != tenantId)
        {
            return false;
        }

        return true;
    }
}
