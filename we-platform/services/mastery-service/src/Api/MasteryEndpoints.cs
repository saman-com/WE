using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MasteryService.Application;
using MasteryService.Domain;
using MasteryService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MasteryService.Api;

public static class MasteryEndpoints
{
    public static void MapMasteryEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/mastery").RequireAuthorization();

        api.MapGet("/students/{studentUserId}", GetStudentMastery);
    }

    private static async Task<IResult> GetStudentMastery(
        string studentUserId,
        ClaimsPrincipal principal,
        MasteryDbContext db,
        IOrganisationAccessChecker accessChecker,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(studentUserId))
        {
            return Results.BadRequest();
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

        var records = await db.Records
            .Where(r => r.StudentUserId == studentUserId)
            .OrderBy(r => r.MicroSkillId)
            .ToListAsync();

        return Results.Ok(new StudentMasteryResponse(
            studentUserId,
            records.Select(ToResponse).ToList()));
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
}
