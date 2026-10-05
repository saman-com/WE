using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AssessmentService.Application;
using AssessmentService.Domain;
using AssessmentService.Infrastructure.Ai;
using AssessmentService.Infrastructure.Data;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace AssessmentService.Api;

public static class AssessmentEndpoints
{
    public static void MapAssessmentEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/assessments").RequireAuthorization();

        api.MapPost("/", CreateAssessment);
        api.MapGet("/", ListAssessments);
        api.MapGet("/class-summary", ListClassAssessmentSummary);
        api.MapGet("/student-summary", ListStudentAssessmentSummary);
        api.MapGet("/{assessmentId:guid}", GetAssessment);
        api.MapPut("/{assessmentId:guid}", UpdateAssessment);
        api.MapPost("/{assessmentId:guid}/publish", PublishAssessment);
        api.MapDelete("/{assessmentId:guid}", DeleteAssessment);
        api.MapPost("/{assessmentId:guid}/submissions", SubmitAssessment);
        api.MapGet("/{assessmentId:guid}/submissions", ListSubmissions);
        api.MapGet("/{assessmentId:guid}/submissions/me", GetMySubmission);
        api.MapGet("/{assessmentId:guid}/submissions/{submissionId:guid}", GetSubmission);
        api.MapPost("/{assessmentId:guid}/submissions/{submissionId:guid}/ai-feedback-draft", RequestAiFeedbackDraft);
        api.MapPost("/ai-feedback-audit/{auditLogId:guid}/finalize", FinalizeAiFeedbackAudit);
    }

    private static async Task<IResult> CreateAssessment(
        CreateAssessmentRequest request,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        if (!CanManageAssessments(principal))
        {
            return Results.Forbid();
        }

        if (!tenantContext.HasTenant || request.OrganisationId != tenantContext.TenantId)
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest(new ApiErrorResponse("validation.invalid_request"));
        }

        var access = await EvaluateTeacherClassAccessAsync(
            principal,
            request.OrganisationId,
            request.ClassId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        var now = DateTimeOffset.UtcNow;
        var assessment = new Assessment
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.OrganisationId,
            OrganisationId = request.OrganisationId,
            ClassId = request.ClassId,
            CreatedByTeacherUserId = principal.UserId(),
            Title = request.Title.Trim(),
            Instructions = string.IsNullOrWhiteSpace(request.Instructions) ? null : request.Instructions.Trim(),
            DueAt = request.DueAt,
            Status = AssessmentStatuses.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        ApplyCurriculumLinks(assessment, request.LearningObjectiveIds, request.MicroSkillIds);
        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        var saved = await LoadAssessmentAsync(db, assessment.Id);
        return Results.Created($"/api/v1/assessments/{assessment.Id}", ToResponse(saved!));
    }

    private static async Task<IResult> ListAssessments(
        Guid? organisationId,
        Guid? classId,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        HttpContext httpContext)
    {
        if (principal.IsStudent() && !classId.HasValue)
        {
            return Results.BadRequest();
        }

        var query = db.Assessments
            .Include(a => a.LearningObjectives)
            .Include(a => a.MicroSkills)
            .AsQueryable();

        if (organisationId.HasValue)
        {
            query = query.Where(a => a.OrganisationId == organisationId.Value);
        }

        if (classId.HasValue)
        {
            query = query.Where(a => a.ClassId == classId.Value);
        }

        if (principal.IsStudent())
        {
            query = query.Where(a => a.Status == AssessmentStatuses.Published);
        }

        var assessments = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        var visible = new List<Assessment>();

        foreach (var assessment in assessments)
        {
            var access = await EvaluateViewAccessAsync(
                principal,
                assessment.OrganisationId,
                assessment.ClassId,
                assessment.Status,
                accessChecker,
                httpContext.Request.Headers.Authorization.ToString());
            if (access is null)
            {
                visible.Add(assessment);
            }
        }

        return Results.Ok(visible.Select(ToResponse).ToList());
    }

    private static async Task<IResult> ListClassAssessmentSummary(
        Guid organisationId,
        Guid classId,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        if (principal.IsSchoolLeader())
        {
            if (!tenantContext.HasTenant || organisationId != tenantContext.TenantId)
            {
                return Results.Forbid();
            }
        }
        else if (!principal.IsTeacher() && !principal.IsAdmin())
        {
            return Results.Forbid();
        }
        else
        {
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
        }

        var assessments = await db.Assessments
            .Include(a => a.Submissions)
            .Where(a => a.OrganisationId == organisationId && a.ClassId == classId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();

        var summaries = assessments
            .Select(a => new ClassAssessmentSummaryResponse(
                a.Id,
                a.Title,
                a.Status,
                a.DueAt,
                a.Submissions.Count,
                0))
            .ToList();

        return Results.Ok(summaries);
    }

    private static async Task<IResult> ListStudentAssessmentSummary(
        Guid organisationId,
        Guid classId,
        string? studentUserId,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        IParentAccessChecker parentAccessChecker,
        HttpContext httpContext)
    {
        string targetStudentUserId;
        if (principal.IsParent())
        {
            if (string.IsNullOrWhiteSpace(studentUserId))
            {
                return Results.BadRequest();
            }

            var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
            if (bearerToken is null)
            {
                return Results.Forbid();
            }

            var allowed = await parentAccessChecker.ParentCanViewStudentAsync(
                principal.UserId(),
                studentUserId,
                bearerToken);
            if (!allowed)
            {
                return Results.Forbid();
            }

            targetStudentUserId = studentUserId;
        }
        else if (principal.IsStudent())
        {
            var access = await EvaluateViewAccessAsync(
                principal,
                organisationId,
                classId,
                AssessmentStatuses.Published,
                accessChecker,
                httpContext.Request.Headers.Authorization.ToString());
            if (access is not null)
            {
                return access;
            }

            targetStudentUserId = principal.UserId();
        }
        else
        {
            return Results.Forbid();
        }

        var assessments = await db.Assessments
            .Include(a => a.LearningObjectives)
            .Include(a => a.Submissions)
            .Where(a =>
                a.OrganisationId == organisationId
                && a.ClassId == classId
                && a.Status == AssessmentStatuses.Published)
            .OrderByDescending(a => a.DueAt ?? a.PublishedAt ?? a.CreatedAt)
            .ToListAsync();

        var summaries = assessments
            .Select(a =>
            {
                var submission = a.Submissions.FirstOrDefault(s => s.StudentUserId == targetStudentUserId);
                return new StudentAssessmentSummaryResponse(
                    a.Id,
                    a.Title,
                    a.DueAt,
                    a.LearningObjectives.Select(l => l.LearningObjectiveId).ToList(),
                    submission is not null,
                    submission?.SubmittedAt);
            })
            .ToList();

        return Results.Ok(summaries);
    }

    private static async Task<IResult> GetAssessment(
        Guid assessmentId,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        var assessment = await LoadAssessmentAsync(db, assessmentId);
        if (assessment is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, assessment);
        if (tenantAccess is not null)
        {
            return tenantAccess;
        }

        var access = await EvaluateViewAccessAsync(
            principal,
            assessment.OrganisationId,
            assessment.ClassId,
            assessment.Status,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        return Results.Ok(ToResponse(assessment));
    }

    private static async Task<IResult> UpdateAssessment(
        Guid assessmentId,
        UpdateAssessmentRequest request,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        HttpContext httpContext)
    {
        if (!CanManageAssessments(principal))
        {
            return Results.Forbid();
        }

        var assessment = await LoadAssessmentAsync(db, assessmentId);
        if (assessment is null)
        {
            return Results.NotFound();
        }

        if (assessment.Status != AssessmentStatuses.Draft)
        {
            return Results.Conflict();
        }

        var access = await EvaluateTeacherClassAccessAsync(
            principal,
            assessment.OrganisationId,
            assessment.ClassId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest(new ApiErrorResponse("validation.invalid_request"));
        }

        assessment.Title = request.Title.Trim();
        assessment.Instructions = string.IsNullOrWhiteSpace(request.Instructions) ? null : request.Instructions.Trim();
        assessment.DueAt = request.DueAt;
        assessment.UpdatedAt = DateTimeOffset.UtcNow;

        db.AssessmentLearningObjectives.RemoveRange(assessment.LearningObjectives);
        db.AssessmentMicroSkills.RemoveRange(assessment.MicroSkills);
        assessment.LearningObjectives.Clear();
        assessment.MicroSkills.Clear();
        ApplyCurriculumLinks(assessment, request.LearningObjectiveIds, request.MicroSkillIds);

        await db.SaveChangesAsync();

        var saved = await LoadAssessmentAsync(db, assessmentId);
        return Results.Ok(ToResponse(saved!));
    }

    private static async Task<IResult> PublishAssessment(
        Guid assessmentId,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        IDomainEventPublisher eventPublisher,
        HttpContext httpContext)
    {
        if (!CanManageAssessments(principal))
        {
            return Results.Forbid();
        }

        var assessment = await LoadAssessmentAsync(db, assessmentId);
        if (assessment is null)
        {
            return Results.NotFound();
        }

        if (assessment.Status != AssessmentStatuses.Draft)
        {
            return Results.Conflict();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var access = await EvaluateTeacherClassAccessAsync(
            principal,
            assessment.OrganisationId,
            assessment.ClassId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        var now = DateTimeOffset.UtcNow;
        assessment.Status = AssessmentStatuses.Published;
        assessment.PublishedAt = now;
        assessment.UpdatedAt = now;
        await db.SaveChangesAsync();

        var recipientUserIds = await accessChecker.GetClassStudentUserIdsAsync(
            assessment.OrganisationId,
            assessment.ClassId,
            bearerToken);
        if (recipientUserIds.Count > 0)
        {
            var eventId = Guid.CreateVersion7();
            await eventPublisher.PublishAssessmentPublishedAsync(new AssessmentPublished(
                eventId,
                eventId,
                now,
                assessment.OrganisationId,
                AssessmentPublished.CurrentVersion,
                assessment.Id,
                assessment.ClassId,
                assessment.Title,
                principal.UserId(),
                recipientUserIds));
        }

        var saved = await LoadAssessmentAsync(db, assessmentId);
        return Results.Ok(ToResponse(saved!));
    }

    private static async Task<IResult> DeleteAssessment(
        Guid assessmentId,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        HttpContext httpContext)
    {
        if (!CanManageAssessments(principal))
        {
            return Results.Forbid();
        }

        var assessment = await LoadAssessmentAsync(db, assessmentId);
        if (assessment is null)
        {
            return Results.NotFound();
        }

        if (assessment.Status != AssessmentStatuses.Draft)
        {
            return Results.Conflict();
        }

        var access = await EvaluateTeacherClassAccessAsync(
            principal,
            assessment.OrganisationId,
            assessment.ClassId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        db.Assessments.Remove(assessment);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // Late submissions are accepted after the due date and flagged with IsLate = true
    // so teachers can review them separately. Students cannot edit after submit.
    private static async Task<IResult> SubmitAssessment(
        Guid assessmentId,
        SubmitAssessmentRequest request,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        HttpContext httpContext)
    {
        if (!principal.IsStudent())
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Responses))
        {
            return Results.BadRequest();
        }

        var assessment = await LoadAssessmentAsync(db, assessmentId);
        if (assessment is null || assessment.Status != AssessmentStatuses.Published)
        {
            return Results.NotFound();
        }

        var access = await EvaluateViewAccessAsync(
            principal,
            assessment.OrganisationId,
            assessment.ClassId,
            assessment.Status,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        var studentUserId = principal.UserId();
        var existing = await db.Submissions.FirstOrDefaultAsync(
            s => s.AssessmentId == assessmentId && s.StudentUserId == studentUserId);
        if (existing is not null)
        {
            return Results.Conflict();
        }

        var now = DateTimeOffset.UtcNow;
        var isLate = assessment.DueAt.HasValue && now > assessment.DueAt.Value;
        var submission = new AssessmentSubmission
        {
            Id = Guid.CreateVersion7(),
            AssessmentId = assessmentId,
            StudentUserId = studentUserId,
            Responses = request.Responses.Trim(),
            Status = SubmissionStatuses.Submitted,
            IsLate = isLate,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Submissions.Add(submission);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/v1/assessments/{assessmentId}/submissions/{submission.Id}",
            ToSubmissionResponse(submission));
    }

    private static async Task<IResult> ListSubmissions(
        Guid assessmentId,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        HttpContext httpContext)
    {
        if (!CanManageAssessments(principal))
        {
            return Results.Forbid();
        }

        var assessment = await LoadAssessmentAsync(db, assessmentId);
        if (assessment is null)
        {
            return Results.NotFound();
        }

        var teacherAccess = await EvaluateTeacherClassAccessAsync(
            principal,
            assessment.OrganisationId,
            assessment.ClassId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (teacherAccess is not null)
        {
            return teacherAccess;
        }

        var submissions = await db.Submissions
            .Where(s => s.AssessmentId == assessmentId)
            .OrderBy(s => s.SubmittedAt)
            .ToListAsync();

        return Results.Ok(submissions.Select(ToSubmissionResponse).ToList());
    }

    private static async Task<IResult> GetMySubmission(
        Guid assessmentId,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        HttpContext httpContext)
    {
        if (!principal.IsStudent())
        {
            return Results.Forbid();
        }

        var assessment = await LoadAssessmentAsync(db, assessmentId);
        if (assessment is null || assessment.Status != AssessmentStatuses.Published)
        {
            return Results.NotFound();
        }

        var access = await EvaluateViewAccessAsync(
            principal,
            assessment.OrganisationId,
            assessment.ClassId,
            assessment.Status,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        var submission = await db.Submissions.FirstOrDefaultAsync(
            s => s.AssessmentId == assessmentId && s.StudentUserId == principal.UserId());
        if (submission is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToSubmissionResponse(submission));
    }

    private static async Task<IResult> GetSubmission(
        Guid assessmentId,
        Guid submissionId,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        IClassAccessChecker accessChecker,
        HttpContext httpContext)
    {
        var assessment = await LoadAssessmentAsync(db, assessmentId);
        if (assessment is null)
        {
            return Results.NotFound();
        }

        var submission = await db.Submissions.FirstOrDefaultAsync(
            s => s.Id == submissionId && s.AssessmentId == assessmentId);
        if (submission is null)
        {
            return Results.NotFound();
        }

        if (principal.IsStudent())
        {
            if (submission.StudentUserId != principal.UserId())
            {
                return Results.Forbid();
            }

            var access = await EvaluateViewAccessAsync(
                principal,
                assessment.OrganisationId,
                assessment.ClassId,
                assessment.Status,
                accessChecker,
                httpContext.Request.Headers.Authorization.ToString());
            if (access is not null)
            {
                return access;
            }

            return Results.Ok(ToSubmissionResponse(submission));
        }

        if (CanManageAssessments(principal))
        {
            var teacherAccess = await EvaluateTeacherClassAccessAsync(
                principal,
                assessment.OrganisationId,
                assessment.ClassId,
                accessChecker,
                httpContext.Request.Headers.Authorization.ToString());
            if (teacherAccess is not null)
            {
                return teacherAccess;
            }

            return Results.Ok(ToSubmissionResponse(submission));
        }

        return Results.Forbid();
    }

    private static async Task<IResult> RequestAiFeedbackDraft(
        Guid assessmentId,
        Guid submissionId,
        RequestAiFeedbackDraftRequest request,
        ClaimsPrincipal principal,
        AssessmentDbContext db,
        AiFeedbackDraftService draftService,
        IClassAccessChecker accessChecker,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!principal.IsTeacher())
        {
            return Results.Forbid();
        }

        if (request.MicroSkillId == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var assessment = await LoadAssessmentAsync(db, assessmentId);
        if (assessment is null)
        {
            return Results.NotFound();
        }

        var teacherAccess = await EvaluateTeacherClassAccessAsync(
            principal,
            assessment.OrganisationId,
            assessment.ClassId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (teacherAccess is not null)
        {
            return teacherAccess;
        }

        var draft = await draftService.RequestDraftAsync(
            assessmentId,
            submissionId,
            request.MicroSkillId,
            principal.UserId(),
            cancellationToken);

        return draft is null ? Results.NotFound() : Results.Ok(draft);
    }

    private static async Task<IResult> FinalizeAiFeedbackAudit(
        Guid auditLogId,
        FinalizeAiFeedbackAuditRequest request,
        ClaimsPrincipal principal,
        AiFeedbackDraftService draftService,
        CancellationToken cancellationToken)
    {
        if (!principal.IsTeacher())
        {
            return Results.Forbid();
        }

        if (request.EvidenceId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.TeacherEditedFeedback))
        {
            return Results.BadRequest();
        }

        var finalized = await draftService.FinalizeAuditAsync(
            auditLogId,
            principal.UserId(),
            request.TeacherEditedFeedback,
            request.EvidenceId,
            cancellationToken);

        return finalized is null ? Results.NotFound() : Results.Ok(finalized);
    }

    private static void ApplyCurriculumLinks(
        Assessment assessment,
        IReadOnlyList<Guid> learningObjectiveIds,
        IReadOnlyList<Guid> microSkillIds)
    {
        foreach (var learningObjectiveId in learningObjectiveIds.Distinct())
        {
            assessment.LearningObjectives.Add(new AssessmentLearningObjective
            {
                AssessmentId = assessment.Id,
                LearningObjectiveId = learningObjectiveId
            });
        }

        foreach (var microSkillId in microSkillIds.Distinct())
        {
            assessment.MicroSkills.Add(new AssessmentMicroSkill
            {
                AssessmentId = assessment.Id,
                MicroSkillId = microSkillId
            });
        }
    }

    private static async Task<Assessment?> LoadAssessmentAsync(AssessmentDbContext db, Guid assessmentId) =>
        await db.Assessments
            .IgnoreQueryFilters()
            .Include(a => a.LearningObjectives)
            .Include(a => a.MicroSkills)
            .FirstOrDefaultAsync(a => a.Id == assessmentId);

    private static async Task<AssessmentSubmission?> LoadSubmissionAsync(
        AssessmentDbContext db,
        Guid assessmentId,
        Guid submissionId) =>
        await db.Submissions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.AssessmentId == assessmentId);

    private static AssessmentResponse ToResponse(Assessment assessment) =>
        new(
            assessment.Id,
            assessment.OrganisationId,
            assessment.ClassId,
            assessment.Title,
            assessment.Instructions,
            assessment.DueAt,
            assessment.Status,
            assessment.PublishedAt,
            assessment.LearningObjectives.Select(l => l.LearningObjectiveId).ToList(),
            assessment.MicroSkills.Select(m => m.MicroSkillId).ToList(),
            assessment.CreatedByTeacherUserId,
            assessment.CreatedAt,
            assessment.UpdatedAt);

    private static SubmissionResponse ToSubmissionResponse(AssessmentSubmission submission) =>
        new(
            submission.Id,
            submission.AssessmentId,
            submission.StudentUserId,
            submission.Responses,
            submission.Status,
            submission.IsLate,
            submission.SubmittedAt,
            submission.CreatedAt,
            submission.UpdatedAt);

    private static bool CanManageAssessments(ClaimsPrincipal principal) =>
        principal.IsAdmin() || principal.IsTeacher();

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

    private static async Task<IResult?> EvaluateViewAccessAsync(
        ClaimsPrincipal principal,
        Guid organisationId,
        Guid classId,
        string status,
        IClassAccessChecker accessChecker,
        string authorizationHeader)
    {
        if (principal.IsAdmin())
        {
            return null;
        }

        if (principal.IsStudent())
        {
            if (status != AssessmentStatuses.Published)
            {
                return Results.NotFound();
            }

            var token = ExtractBearerToken(authorizationHeader);
            if (token is null)
            {
                return Results.Forbid();
            }

            var enrolled = await accessChecker.StudentIsEnrolledInClassAsync(
                principal.UserId(),
                organisationId,
                classId,
                token);
            return enrolled ? null : Results.Forbid();
        }

        if (principal.IsTeacher())
        {
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

        return Results.Forbid();
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

    private static bool IsStudent(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Student);

    private static bool IsParent(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Parent);
}
