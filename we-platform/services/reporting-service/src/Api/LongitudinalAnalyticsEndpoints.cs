using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ReportingService.Application;
using ReportingService.Domain;

namespace ReportingService.Api;

public static class LongitudinalAnalyticsEndpoints
{
    public static void MapLongitudinalAnalyticsEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/analytics").RequireAuthorization();

        api.MapGet(
            "/organisations/{organisationId:guid}/students/{studentUserId}/longitudinal",
            GetStudentLongitudinal);
        api.MapGet(
            "/organisations/{organisationId:guid}/longitudinal",
            GetOrganisationLongitudinal);
    }

    private static async Task<IResult> GetStudentLongitudinal(
        Guid organisationId,
        string studentUserId,
        ClaimsPrincipal principal,
        IOrganisationAccessChecker accessChecker,
        ILongitudinalAnalyticsQuery analyticsQuery,
        HttpContext httpContext,
        CancellationToken cancellationToken)
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
            var allowed = await accessChecker.TeacherCanViewStudentAsync(
                principal.UserId(),
                studentUserId,
                bearerToken,
                cancellationToken);
            if (!allowed)
            {
                return Results.Forbid();
            }
        }

        var response = await analyticsQuery.GetStudentLongitudinalAsync(
            organisationId,
            studentUserId,
            cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetOrganisationLongitudinal(
        Guid organisationId,
        ClaimsPrincipal principal,
        IOrganisationAccessChecker accessChecker,
        ILongitudinalAnalyticsQuery analyticsQuery,
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

        var response = await analyticsQuery.GetOrganisationLongitudinalAsync(
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

    private static bool IsTeacher(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Teacher);

    private static bool IsSchoolLeader(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.SchoolLeader);
}
