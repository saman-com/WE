using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DiagnosticService.Application;
using DiagnosticService.Domain;
using DiagnosticService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace DiagnosticService.Api;

public static class DiagnosticEndpoints
{
    public static void MapDiagnosticEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/diagnostics").RequireAuthorization();

        api.MapGet("/students/{studentUserId}", GetStudentDiagnostics);
    }

    private static async Task<IResult> GetStudentDiagnostics(
        string studentUserId,
        ClaimsPrincipal principal,
        DiagnosticDbContext db,
        IOrganisationAccessChecker accessChecker,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(studentUserId))
        {
            return Results.BadRequest();
        }

        if (!tenantContext.HasTenant)
        {
            return Results.Forbid();
        }

        var access = await EvaluateAccessAsync(
            principal,
            studentUserId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        var tenantId = tenantContext.TenantId!.Value;
        var allDiagnostics = await db.Diagnostics
            .IgnoreQueryFilters()
            .Where(d => d.StudentUserId == studentUserId)
            .OrderBy(d => d.CreatedAt)
            .ThenBy(d => d.MicroSkillId)
            .ToListAsync();

        if (allDiagnostics.Count > 0 && allDiagnostics.All(d => d.TenantId != tenantId))
        {
            return Results.Forbid();
        }

        var diagnostics = allDiagnostics
            .Where(d => d.TenantId == tenantId)
            .ToList();

        return Results.Ok(new StudentDiagnosticsResponse(
            studentUserId,
            diagnostics.Select(ToResponse).ToList()));
    }

    private static async Task<IResult?> EvaluateAccessAsync(
        ClaimsPrincipal principal,
        string studentUserId,
        IOrganisationAccessChecker accessChecker,
        string authorizationHeader)
    {
        if (principal.IsAdmin())
        {
            return null;
        }

        var userId = principal.UserId();
        if (principal.IsTeacher())
        {
            var token = ExtractBearerToken(authorizationHeader);
            if (token is null)
            {
                return Results.Forbid();
            }

            var allowed = await accessChecker.TeacherCanViewStudentAsync(userId, studentUserId, token);
            return allowed ? null : Results.Forbid();
        }

        if (principal.IsSchoolLeader())
        {
            var token = ExtractBearerToken(authorizationHeader);
            if (token is null)
            {
                return Results.Forbid();
            }

            var allowed = await accessChecker.SchoolLeaderCanViewStudentAsync(userId, studentUserId, token);
            return allowed ? null : Results.Forbid();
        }

        return Results.Forbid();
    }

    private static MicroSkillDiagnosticResponse ToResponse(MicroSkillDiagnostic diagnostic) =>
        new(
            diagnostic.Id,
            diagnostic.EvidenceId,
            diagnostic.AssessmentId,
            diagnostic.MicroSkillId,
            diagnostic.Status,
            diagnostic.Mark,
            diagnostic.Reason,
            diagnostic.CreatedAt);

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
