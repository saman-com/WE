using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrganisationService.Application;
using OrganisationService.Domain;
using OrganisationService.Infrastructure.Data;

namespace OrganisationService.Api;

public static class StudentWorkspaceEndpoints
{
    public static void MapStudentWorkspaceEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/students").RequireAuthorization();

        api.MapGet("/{studentUserId}/workspace", GetStudentWorkspace);
    }

    private static async Task<IResult> GetStudentWorkspace(
        string studentUserId,
        ClaimsPrincipal principal,
        OrganisationDbContext db,
        IAssessmentDashboardClient assessmentClient,
        IEvidenceDashboardClient evidenceClient,
        IStudentLearningProfileClient profileClient,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(studentUserId))
        {
            return Results.BadRequest();
        }

        if (!principal.IsStudent() && !principal.IsAdmin())
        {
            return Results.Forbid();
        }

        if (principal.IsStudent() && principal.UserId() != studentUserId)
        {
            return Results.Forbid();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var enrollments = await db.ClassEnrollments
            .Include(e => e.Class)
            .Where(e => e.StudentUserId == studentUserId)
            .OrderBy(e => e.Class.Name)
            .ToListAsync();

        var assessmentTasks = enrollments
            .Select(async enrollment =>
            {
                var summaries = await assessmentClient.ListStudentAssessmentSummariesAsync(
                    enrollment.Class.OrganisationId,
                    enrollment.ClassId,
                    bearerToken);
                return summaries.Select(summary => new StudentWorkspaceAssessmentSummary(
                    summary.Id,
                    enrollment.Class.OrganisationId,
                    enrollment.ClassId,
                    enrollment.Class.Name,
                    summary.Title,
                    summary.DueAt,
                    summary.LearningObjectiveIds,
                    summary.HasSubmitted,
                    summary.SubmittedAt));
            })
            .ToList();
        var assessmentGroups = await Task.WhenAll(assessmentTasks);
        var assessments = assessmentGroups.SelectMany(item => item).ToList();

        var feedbackItems = await evidenceClient.ListStudentFeedbackAsync(bearerToken);
        var feedback = feedbackItems
            .Select(item => new StudentWorkspaceFeedbackSummary(
                item.Id,
                item.AssessmentId,
                item.Title,
                item.ApprovedAt,
                item.MicroSkillMarks
                    .Select(mark => new StudentWorkspaceFeedbackMark(
                        mark.MicroSkillId,
                        mark.Mark,
                        mark.Feedback))
                    .ToList()))
            .ToList();

        var profile = await profileClient.GetProfileAsync(studentUserId, bearerToken);
        var timeline = profile?.EvidenceTimeline
            .Select(entry => new StudentWorkspaceTimelineEntry(
                entry.Id,
                entry.Title,
                entry.RecordedAt))
            .ToList()
            ?? [];

        return Results.Ok(new StudentWorkspaceResponse(
            studentUserId,
            assessments,
            feedback,
            timeline));
    }

    private static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? string.Empty;

    private static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.SystemAdministrator);

    private static bool IsStudent(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Student);

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
}
