using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EvidenceService.Application;
using EvidenceService.Domain;
using EvidenceService.Infrastructure.Data;
using EvidenceService.Infrastructure.Messaging;

namespace EvidenceService.Api;

public static class EvidenceEndpoints
{
    public static void MapEvidenceEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/evidence").RequireAuthorization();

        api.MapPost("/", ApproveEvidence);
        api.MapGet("/", ListEvidence);
        api.MapGet("/class-summary", ListClassEvidenceSummary);
        api.MapGet("/student-feedback", ListStudentFeedback);
        api.MapGet("/{evidenceId:guid}", GetEvidence);
        api.MapPut("/{evidenceId:guid}", RejectMutation);
        api.MapDelete("/{evidenceId:guid}", RejectMutation);
    }

    private static async Task<IResult> ApproveEvidence(
        ApproveEvidenceRequest request,
        ClaimsPrincipal principal,
        EvidenceDbContext db,
        IClassAccessChecker accessChecker,
        IStudentLearningProfileClient profileClient,
        IDomainEventPublisher eventPublisher,
        HttpContext httpContext)
    {
        if (!principal.IsTeacher())
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Title)
            || string.IsNullOrWhiteSpace(request.StudentUserId)
            || request.MicroSkillMarks is not { Count: > 0 })
        {
            return Results.BadRequest();
        }

        var token = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (token is null)
        {
            return Results.Forbid();
        }

        var allowed = await accessChecker.TeacherCanManageClassAsync(
            principal.UserId(),
            request.OrganisationId,
            request.ClassId,
            token);
        if (!allowed)
        {
            return Results.Forbid();
        }

        var duplicate = await db.Evidence.AnyAsync(e => e.SubmissionId == request.SubmissionId);
        if (duplicate)
        {
            return Results.Conflict();
        }

        var now = DateTimeOffset.UtcNow;
        var evidence = new EducationalEvidence
        {
            Id = Guid.CreateVersion7(),
            OrganisationId = request.OrganisationId,
            ClassId = request.ClassId,
            AssessmentId = request.AssessmentId,
            SubmissionId = request.SubmissionId,
            StudentUserId = request.StudentUserId,
            Title = request.Title.Trim(),
            Status = EvidenceStatuses.Approved,
            ApprovedByTeacherUserId = principal.UserId(),
            ApprovedAt = now,
            CreatedAt = now
        };

        foreach (var mark in request.MicroSkillMarks)
        {
            evidence.MicroSkillMarks.Add(new EvidenceMicroSkillMark
            {
                EvidenceId = evidence.Id,
                MicroSkillId = mark.MicroSkillId,
                Mark = mark.Mark,
                Feedback = mark.Feedback?.Trim() ?? string.Empty
            });
        }

        db.Evidence.Add(evidence);
        await db.SaveChangesAsync();

        await eventPublisher.PublishEvidenceCreatedAsync(EvidenceApprovalEventFactory.CreateEvidenceCreated(evidence));
        await eventPublisher.PublishAssessmentApprovedAsync(EvidenceApprovalEventFactory.CreateAssessmentApproved(evidence));

        await profileClient.RecordEvidenceAsync(
            evidence.StudentUserId,
            evidence.Id,
            evidence.AssessmentId,
            evidence.MicroSkillMarks.Select(m => m.MicroSkillId).ToList(),
            evidence.Title,
            evidence.ApprovedAt,
            token);

        var saved = await LoadEvidenceAsync(db, evidence.Id);
        return Results.Created($"/api/v1/evidence/{evidence.Id}", ToResponse(saved!));
    }

    private static async Task<IResult> ListEvidence(
        Guid? assessmentId,
        ClaimsPrincipal principal,
        EvidenceDbContext db,
        IClassAccessChecker accessChecker,
        HttpContext httpContext)
    {
        if (!principal.IsTeacher() && !principal.IsAdmin())
        {
            return Results.Forbid();
        }

        if (!assessmentId.HasValue)
        {
            return Results.BadRequest();
        }

        var items = await db.Evidence
            .Include(e => e.MicroSkillMarks)
            .Where(e => e.AssessmentId == assessmentId.Value)
            .OrderBy(e => e.ApprovedAt)
            .ToListAsync();

        var visible = new List<EducationalEvidence>();
        foreach (var evidence in items)
        {
            var access = await EvaluateTeacherClassAccessAsync(
                principal,
                evidence.OrganisationId,
                evidence.ClassId,
                accessChecker,
                httpContext.Request.Headers.Authorization.ToString());
            if (access is null)
            {
                visible.Add(evidence);
            }
        }

        return Results.Ok(visible.Select(ToResponse).ToList());
    }

    private static async Task<IResult> ListClassEvidenceSummary(
        Guid organisationId,
        Guid classId,
        ClaimsPrincipal principal,
        EvidenceDbContext db,
        IClassAccessChecker accessChecker,
        HttpContext httpContext)
    {
        if (!principal.IsTeacher() && !principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var access = await EvaluateTeacherClassAccessAsync(
            principal,
            organisationId,
            classId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        var summaries = await db.Evidence
            .Where(e => e.OrganisationId == organisationId && e.ClassId == classId)
            .GroupBy(e => e.AssessmentId)
            .Select(group => new ClassEvidenceSummaryResponse(group.Key, group.Count()))
            .ToListAsync();

        return Results.Ok(summaries);
    }

    private static async Task<IResult> ListStudentFeedback(
        ClaimsPrincipal principal,
        EvidenceDbContext db)
    {
        if (!principal.IsStudent())
        {
            return Results.Forbid();
        }

        var studentUserId = principal.UserId();
        var items = await db.Evidence
            .Include(e => e.MicroSkillMarks)
            .Where(e => e.StudentUserId == studentUserId && e.Status == EvidenceStatuses.Approved)
            .OrderByDescending(e => e.ApprovedAt)
            .ToListAsync();

        var feedback = items
            .Select(e => new StudentFeedbackResponse(
                e.Id,
                e.AssessmentId,
                e.Title,
                e.ApprovedAt,
                e.MicroSkillMarks
                    .Select(m => new MicroSkillMarkResponse(m.MicroSkillId, m.Mark, m.Feedback))
                    .ToList()))
            .ToList();

        return Results.Ok(feedback);
    }

    private static async Task<IResult> GetEvidence(
        Guid evidenceId,
        ClaimsPrincipal principal,
        EvidenceDbContext db,
        IClassAccessChecker accessChecker,
        HttpContext httpContext)
    {
        var evidence = await LoadEvidenceAsync(db, evidenceId);
        if (evidence is null)
        {
            return Results.NotFound();
        }

        if (principal.IsStudent())
        {
            if (evidence.StudentUserId != principal.UserId())
            {
                return Results.Forbid();
            }

            return Results.Ok(ToResponse(evidence));
        }

        var access = await EvaluateTeacherClassAccessAsync(
            principal,
            evidence.OrganisationId,
            evidence.ClassId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        return Results.Ok(ToResponse(evidence));
    }

    private static async Task<IResult> RejectMutation(
        Guid evidenceId,
        EvidenceDbContext db)
    {
        var evidence = await db.Evidence.AsNoTracking().FirstOrDefaultAsync(e => e.Id == evidenceId);
        if (evidence is null)
        {
            return Results.NotFound();
        }

        return Results.Forbid();
    }

    private static async Task<EducationalEvidence?> LoadEvidenceAsync(EvidenceDbContext db, Guid evidenceId) =>
        await db.Evidence
            .Include(e => e.MicroSkillMarks)
            .FirstOrDefaultAsync(e => e.Id == evidenceId);

    private static EvidenceResponse ToResponse(EducationalEvidence evidence) =>
        new(
            evidence.Id,
            evidence.AssessmentId,
            evidence.SubmissionId,
            evidence.StudentUserId,
            evidence.Title,
            evidence.Status,
            evidence.MicroSkillMarks
                .Select(m => new MicroSkillMarkResponse(m.MicroSkillId, m.Mark, m.Feedback))
                .ToList(),
            evidence.ApprovedByTeacherUserId,
            evidence.ApprovedAt);

    private static async Task<IResult?> EvaluateTeacherClassAccessAsync(
        ClaimsPrincipal principal,
        Guid organisationId,
        Guid classId,
        IClassAccessChecker accessChecker,
        string authorizationHeader)
    {
        if (principal.IsAdmin())
        {
            return null;
        }

        if (!principal.IsTeacher())
        {
            return Results.Forbid();
        }

        var token = ExtractBearerToken(authorizationHeader);
        if (token is null)
        {
            return Results.Forbid();
        }

        var allowed = await accessChecker.TeacherCanManageClassAsync(
            principal.UserId(),
            organisationId,
            classId,
            token);
        return allowed ? null : Results.Forbid();
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

    private static bool IsStudent(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Student);
}
