using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrganisationService.Application;
using OrganisationService.Domain;
using OrganisationService.Infrastructure.Data;

namespace OrganisationService.Api;

public static class ParentWorkspaceEndpoints
{
    public static void MapParentWorkspaceEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/parents").RequireAuthorization();

        api.MapPost("/{parentUserId}/children", LinkParentToStudent);
        api.MapGet("/{parentUserId}/children", ListParentChildren);
        api.MapDelete("/{parentUserId}/children/{studentUserId}", UnlinkParentFromStudent);
        api.MapGet("/me/children", ListMyChildren);
        api.MapGet("/me/children/{studentUserId}/progress", GetChildProgress);

        app.MapGet("/api/v1/access/parent/{parentUserId}/student/{studentUserId}", CheckParentAccess)
            .RequireAuthorization();
    }

    private static async Task<IResult> LinkParentToStudent(
        string parentUserId,
        LinkParentStudentRequest request,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(parentUserId) || string.IsNullOrWhiteSpace(request.StudentUserId))
        {
            return Results.BadRequest();
        }

        var studentUserId = request.StudentUserId.Trim();
        if (await db.ParentStudentLinks.AnyAsync(link =>
                link.ParentUserId == parentUserId && link.StudentUserId == studentUserId))
        {
            return Results.Conflict();
        }

        var link = new ParentStudentLink
        {
            ParentUserId = parentUserId,
            StudentUserId = studentUserId,
            LinkedAt = DateTimeOffset.UtcNow
        };

        db.ParentStudentLinks.Add(link);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/v1/parents/{parentUserId}/children/{studentUserId}",
            ToLinkResponse(link));
    }

    private static async Task<IResult> ListParentChildren(
        string parentUserId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!CanViewParentLinks(principal, parentUserId))
        {
            return Results.Forbid();
        }

        var children = await db.ParentStudentLinks
            .Where(link => link.ParentUserId == parentUserId)
            .OrderBy(link => link.StudentUserId)
            .Select(link => ToLinkResponse(link))
            .ToListAsync();

        return Results.Ok(children);
    }

    private static async Task<IResult> UnlinkParentFromStudent(
        string parentUserId,
        string studentUserId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var link = await db.ParentStudentLinks.FirstOrDefaultAsync(item =>
            item.ParentUserId == parentUserId && item.StudentUserId == studentUserId);
        if (link is null)
        {
            return Results.NotFound();
        }

        db.ParentStudentLinks.Remove(link);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ListMyChildren(
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsParent())
        {
            return Results.Forbid();
        }

        var parentUserId = principal.UserId();
        var children = await db.ParentStudentLinks
            .Where(link => link.ParentUserId == parentUserId)
            .OrderBy(link => link.StudentUserId)
            .Select(link => ToLinkResponse(link))
            .ToListAsync();

        return Results.Ok(children);
    }

    private static async Task<IResult> GetChildProgress(
        string studentUserId,
        ClaimsPrincipal principal,
        OrganisationDbContext db,
        IAssessmentDashboardClient assessmentClient,
        IEvidenceDashboardClient evidenceClient,
        IMasteryDashboardClient masteryClient,
        IInterventionDashboardClient interventionClient,
        HttpContext httpContext)
    {
        if (!principal.IsParent())
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(studentUserId))
        {
            return Results.BadRequest();
        }

        var parentUserId = principal.UserId();
        var linked = await db.ParentStudentLinks.AnyAsync(link =>
            link.ParentUserId == parentUserId && link.StudentUserId == studentUserId);
        if (!linked)
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
                var summaries = await assessmentClient.ListStudentAssessmentSummariesForStudentAsync(
                    enrollment.Class.OrganisationId,
                    enrollment.ClassId,
                    studentUserId,
                    bearerToken);
                return summaries.Select(summary => new ParentAssessmentSummary(
                    summary.Id,
                    enrollment.Class.Name,
                    summary.Title,
                    summary.DueAt,
                    summary.HasSubmitted,
                    summary.SubmittedAt));
            })
            .ToList();
        var assessmentGroups = await Task.WhenAll(assessmentTasks);
        var assessments = assessmentGroups.SelectMany(item => item).ToList();

        var feedbackItems = await evidenceClient.ListStudentFeedbackForStudentAsync(studentUserId, bearerToken);
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

        var masteryRecords = await masteryClient.ListStudentMasteryAsync(studentUserId, bearerToken);
        var mastery = masteryRecords
            .Select(record => new ParentMasterySummary(record.MicroSkillId, record.MasteryLevel))
            .ToList();

        var interventions = await interventionClient.ListActiveInterventionsAsync(studentUserId, bearerToken);
        var activeInterventions = interventions
            .Where(item => item.Status is "Planned" or "Active")
            .Select(item => new ParentInterventionSummary(
                item.Id,
                item.Summary,
                item.Status,
                item.PlannedStartAt,
                item.PlannedEndAt))
            .ToList();

        return Results.Ok(new ParentChildProgressResponse(
            studentUserId,
            mastery,
            feedback,
            assessments,
            activeInterventions));
    }

    private static async Task<IResult> CheckParentAccess(
        string parentUserId,
        string studentUserId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (principal.IsAdmin())
        {
            return Results.Ok();
        }

        if (principal.IsParent() && principal.UserId() == parentUserId)
        {
            var linked = await db.ParentStudentLinks.AnyAsync(link =>
                link.ParentUserId == parentUserId && link.StudentUserId == studentUserId);
            return linked ? Results.Ok() : Results.Forbid();
        }

        return Results.Forbid();
    }

    private static bool CanViewParentLinks(ClaimsPrincipal principal, string parentUserId) =>
        principal.IsAdmin() || (principal.IsParent() && principal.UserId() == parentUserId);

    private static ParentChildLinkResponse ToLinkResponse(ParentStudentLink link) =>
        new(link.ParentUserId, link.StudentUserId);

    private static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? string.Empty;

    private static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.SystemAdministrator);

    private static bool IsParent(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Parent);

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
