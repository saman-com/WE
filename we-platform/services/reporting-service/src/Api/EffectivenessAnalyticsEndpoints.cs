using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ReportingService.Application;
using ReportingService.Domain;

namespace ReportingService.Api;

public static class EffectivenessAnalyticsEndpoints
{
    public static void MapEffectivenessAnalyticsEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/analytics").RequireAuthorization();

        api.MapGet(
            "/organisations/{organisationId:guid}/effectiveness",
            GetOrganisationEffectiveness);
    }

    private static async Task<IResult> GetOrganisationEffectiveness(
        Guid organisationId,
        ClaimsPrincipal principal,
        IOrganisationAccessChecker accessChecker,
        IEffectivenessAnalyticsQuery analyticsQuery,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!principal.IsSchoolLeader() && !principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        if (principal.IsSchoolLeader())
        {
            var allowed = await accessChecker.SchoolLeaderCanViewOrganisationAsync(
                principal.UserId(),
                organisationId,
                bearerToken,
                cancellationToken);
            if (!allowed)
            {
                return Results.Forbid();
            }
        }

        var response = await analyticsQuery.GetOrganisationEffectivenessAsync(
            organisationId,
            cancellationToken);

        return Results.Ok(response);
    }

    private static string? ExtractBearerToken(string authorizationHeader)
    {
        const string prefix = "Bearer ";
        if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorizationHeader[prefix.Length..].Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    private static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? string.Empty;

    private static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.SystemAdministrator);

    private static bool IsSchoolLeader(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.SchoolLeader);
}
