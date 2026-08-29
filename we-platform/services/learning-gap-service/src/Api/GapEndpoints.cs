using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LearningGapService.Application;
using LearningGapService.Domain;
using LearningGapService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LearningGapService.Api;

public static class GapEndpoints
{
    public static void MapGapEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/gaps").RequireAuthorization();

        api.MapGet("/students/{studentUserId}", GetStudentGaps);
    }

    private static async Task<IResult> GetStudentGaps(
        string studentUserId,
        ClaimsPrincipal principal,
        GapDbContext db,
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

        var gaps = await db.Gaps
            .Where(g => g.StudentUserId == studentUserId)
            .OrderBy(g => g.CreatedAt)
            .ThenBy(g => g.MicroSkillId)
            .ToListAsync();

        return Results.Ok(new StudentLearningGapsResponse(
            studentUserId,
            gaps.Select(ToResponse).ToList()));
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

    private static LearningGapResponse ToResponse(LearningGap gap) =>
        new(
            gap.Id,
            gap.EvidenceId,
            gap.AssessmentId,
            gap.MicroSkillId,
            gap.LearningObjectiveId,
            gap.ExpectedMastery,
            gap.ActualMastery,
            gap.Mark,
            gap.Severity,
            gap.Urgency,
            gap.Explanation,
            gap.CreatedAt);

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
