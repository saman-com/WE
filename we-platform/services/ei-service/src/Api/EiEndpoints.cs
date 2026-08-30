using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EiService.Application;
using EiService.Domain;
using EiService.Infrastructure.Ai;

namespace EiService.Api;

public static class EiEndpoints
{
    public static void MapEiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/ei").RequireAuthorization();

        api.MapGet("/organisations/{organisationId:guid}/classes/{classId:guid}/insights", GetClassInsights);
        api.MapPost(
            "/organisations/{organisationId:guid}/classes/{classId:guid}/lesson-summary-draft",
            RequestLessonSummaryDraft);
        api.MapPost("/students/{studentUserId}/progress-report-draft", RequestProgressReportDraft);
        api.MapPost("/ai-summary-audit/{auditLogId:guid}/finalize", FinalizeAiSummaryAudit);
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

    private static async Task<IResult> RequestLessonSummaryDraft(
        Guid organisationId,
        Guid classId,
        RequestLessonSummaryDraftRequest request,
        ClaimsPrincipal principal,
        IClassAccessChecker accessChecker,
        AiSummaryDraftService draftService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!principal.IsTeacher())
        {
            return Results.Forbid();
        }

        if (request.UnitId == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var allowed = await accessChecker.TeacherCanManageClassAsync(
            principal.UserId(),
            organisationId,
            classId,
            bearerToken,
            cancellationToken);
        if (!allowed)
        {
            return Results.Forbid();
        }

        var draft = await draftService.RequestLessonSummaryDraftAsync(
            organisationId,
            classId,
            request.UnitId,
            principal.UserId(),
            bearerToken,
            cancellationToken);

        return draft is null ? Results.NotFound() : Results.Ok(draft);
    }

    private static async Task<IResult> RequestProgressReportDraft(
        string studentUserId,
        RequestProgressReportDraftRequest request,
        ClaimsPrincipal principal,
        IClassAccessChecker accessChecker,
        AiSummaryDraftService draftService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!principal.IsTeacher())
        {
            return Results.Forbid();
        }

        if (request.OrganisationId == Guid.Empty || request.ClassId == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var allowed = await accessChecker.TeacherCanManageClassAsync(
            principal.UserId(),
            request.OrganisationId,
            request.ClassId,
            bearerToken,
            cancellationToken);
        if (!allowed)
        {
            return Results.Forbid();
        }

        var draft = await draftService.RequestProgressReportDraftAsync(
            studentUserId,
            request.OrganisationId,
            request.ClassId,
            principal.UserId(),
            bearerToken,
            cancellationToken);

        return draft is null ? Results.NotFound() : Results.Ok(draft);
    }

    private static async Task<IResult> FinalizeAiSummaryAudit(
        Guid auditLogId,
        FinalizeAiSummaryAuditRequest request,
        ClaimsPrincipal principal,
        AiSummaryDraftService draftService,
        CancellationToken cancellationToken)
    {
        if (!principal.IsTeacher())
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.TeacherEditedContent))
        {
            return Results.BadRequest();
        }

        var finalized = await draftService.FinalizeAuditAsync(
            auditLogId,
            principal.UserId(),
            request.TeacherEditedContent,
            cancellationToken);

        return finalized is null ? Results.NotFound() : Results.Ok(finalized);
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
