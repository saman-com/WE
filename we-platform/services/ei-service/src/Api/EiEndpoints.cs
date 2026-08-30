using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EiService.Application;
using EiService.Domain;

namespace EiService.Api;

public static class EiEndpoints
{
    public static void MapEiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/ei").RequireAuthorization();

        api.MapGet("/organisations/{organisationId:guid}/classes/{classId:guid}/insights", GetClassInsights);
    }

    private static async Task<IResult> GetClassInsights(
        Guid organisationId,
        Guid classId,
        ClaimsPrincipal principal,
        IClassAccessChecker accessChecker,
        IClassInsightsProvider insightsProvider,
        HttpContext httpContext)
    {
        if (!principal.IsTeacher() && !principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        if (principal.IsTeacher())
        {
            var allowed = await accessChecker.TeacherCanManageClassAsync(
                principal.UserId(),
                organisationId,
                classId,
                bearerToken);
            if (!allowed)
            {
                return Results.Forbid();
            }
        }

        var insights = await insightsProvider.GetClassInsightsAsync(
            organisationId,
            classId,
            bearerToken);
        return insights is null ? Results.NotFound() : Results.Ok(insights);
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

    private static bool IsTeacher(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Teacher);
}
