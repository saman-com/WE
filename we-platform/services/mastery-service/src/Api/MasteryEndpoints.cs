using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MasteryService.Application;
using MasteryService.Domain;
using MasteryService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace MasteryService.Api;

public static class MasteryEndpoints
{
    public static void MapMasteryEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/mastery").RequireAuthorization();

        api.MapGet("/students/{studentUserId}", GetStudentMastery);
        api.MapGet("/students/{studentUserId}/parent-summary", GetParentMasterySummary);
    }

    private static async Task<IResult> GetStudentMastery(
        string studentUserId,
        ClaimsPrincipal principal,
        MasteryDbContext db,
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

        var allRecords = await db.Records
            .IgnoreQueryFilters()
            .Where(r => r.StudentUserId == studentUserId)
            .OrderBy(r => r.MicroSkillId)
            .ToListAsync();

        if (allRecords.Count > 0 && allRecords.All(r => r.TenantId != tenantContext.TenantId))
        {
            return Results.Forbid();
        }

        var records = allRecords
            .Where(r => r.TenantId == tenantContext.TenantId)
            .ToList();

        return Results.Ok(new StudentMasteryResponse(
            studentUserId,
            records.Select(ToResponse).ToList()));
    }

    private static async Task<IResult> GetParentMasterySummary(
        string studentUserId,
        ClaimsPrincipal principal,
        MasteryDbContext db,
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

        if (!principal.IsParent())
        {
            return Results.Forbid();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var allowed = await accessChecker.ParentCanViewStudentAsync(
            principal.UserId(),
            studentUserId,
            bearerToken);
        if (!allowed)
        {
            return Results.Forbid();
        }

        var allRecords = await db.Records
            .IgnoreQueryFilters()
            .Where(r => r.StudentUserId == studentUserId)
            .OrderBy(r => r.MicroSkillId)
            .ToListAsync();

        if (allRecords.Count > 0 && allRecords.All(r => r.TenantId != tenantContext.TenantId))
        {
            return Results.Forbid();
        }

        var records = allRecords
            .Where(r => r.TenantId == tenantContext.TenantId)
            .ToList();

        return Results.Ok(new ParentMasterySummaryResponse(
            studentUserId,
            records.Select(record => new ParentMasteryRecordResponse(
                record.MicroSkillId,
                record.MasteryLevel)).ToList()));
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
        if (principal.IsStudent())
        {
            return userId == studentUserId ? null : Results.Forbid();
        }

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

    private static MasteryRecordResponse ToResponse(MasteryRecord record) =>
        new(
            record.Id,
            record.MicroSkillId,
            record.MasteryLevel,
            record.WeightedAverage,
            record.ConfidenceScore,
            record.EvidenceCount,
            record.Explanation,
            record.CalculatedAt);

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

    private static bool IsStudent(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Student);

    private static bool IsParent(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Parent);
}
