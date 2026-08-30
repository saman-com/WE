using System.Net;
using System.Net.Http.Json;
using AssessmentService.Application;
using AssessmentService.Domain;
using AssessmentService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace AssessmentService.Tests;

public class AiFeedbackDraftEndpointTests : IClassFixture<AssessmentWebApplicationFactory>
{
    private readonly AssessmentWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly FakeClassAccessChecker _accessChecker;
    private readonly FakeAiGatewayClient _aiGateway;

    public AiFeedbackDraftEndpointTests(AssessmentWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _aiGateway = factory.AiGateway;
    }

    [Fact]
    public async Task Teacher_CanRequestAiFeedbackDraft()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "AI feedback quiz",
            null,
            [microSkillId]);
        var submission = await SubmitAssessmentAsync(
            studentId,
            published.Id,
            "Student work on fractions.");

        var draft = await RequestDraftAsync(
            teacherId,
            published.Id,
            submission.Id,
            microSkillId);

        Assert.False(string.IsNullOrWhiteSpace(draft.DraftFeedback));
        Assert.Equal("assessment-feedback", draft.PromptId);
        Assert.Equal("1.0.0", draft.PromptVersion);
        Assert.NotEqual(Guid.Empty, draft.AuditLogId);
        Assert.Single(_aiGateway.Calls);
        Assert.Equal("assessment-feedback", _aiGateway.Calls[0].PromptId);
    }

    [Fact]
    public async Task Student_CannotRequestAiFeedbackDraft()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Protected quiz",
            null,
            [microSkillId]);
        var submission = await SubmitAssessmentAsync(studentId, published.Id, "Student work.");

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/assessments/{published.Id}/submissions/{submission.Id}/ai-feedback-draft",
            studentId,
            TestJwt.StudentRole);
        request.Content = JsonContent.Create(new RequestAiFeedbackDraftRequest(microSkillId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AiDraft_IsNotAutoApplied_SubmissionUnchanged()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "No auto-apply quiz",
            null,
            [microSkillId]);
        var submission = await SubmitAssessmentAsync(
            studentId,
            published.Id,
            "Original student responses.");

        await RequestDraftAsync(teacherId, published.Id, submission.Id, microSkillId);

        var reloaded = await SendAsAsync<SubmissionResponse>(
            HttpMethod.Get,
            $"/api/v1/assessments/{published.Id}/submissions/{submission.Id}",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Equal("Original student responses.", reloaded.Responses);
        Assert.Equal(SubmissionStatuses.Submitted, reloaded.Status);
    }

    [Fact]
    public async Task AuditLog_RecordsPromptAndAiResponse()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Audit quiz",
            null,
            [microSkillId]);
        var submission = await SubmitAssessmentAsync(studentId, published.Id, "Evidence summary text.");

        var draft = await RequestDraftAsync(
            teacherId,
            published.Id,
            submission.Id,
            microSkillId);

        var audit = await LoadAuditLogAsync(draft.AuditLogId);

        Assert.NotNull(audit);
        Assert.Equal("assessment-feedback", audit.PromptId);
        Assert.Equal("1.0.0", audit.PromptVersion);
        Assert.Equal(_aiGateway.DraftContent, audit.AiResponse);
        Assert.Equal(teacherId, audit.TeacherUserId);
        Assert.Null(audit.TeacherEditedFeedback);
        Assert.Null(audit.FinalApprovedAt);
    }

    [Fact]
    public async Task AuditLog_RecordsTeacherEditsAndFinalApproval()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var microSkillId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Finalize audit quiz",
            null,
            [microSkillId]);
        var submission = await SubmitAssessmentAsync(studentId, published.Id, "Student work.");

        var draft = await RequestDraftAsync(
            teacherId,
            published.Id,
            submission.Id,
            microSkillId);

        var editedFeedback = "Edited by teacher before approval.";
        await FinalizeAuditAsync(draft.AuditLogId, teacherId, editedFeedback, evidenceId);

        var audit = await LoadAuditLogAsync(draft.AuditLogId);

        Assert.NotNull(audit);
        Assert.Equal(editedFeedback, audit.TeacherEditedFeedback);
        Assert.Equal(evidenceId, audit.EvidenceId);
        Assert.NotNull(audit.FinalApprovedAt);
    }

    [Fact]
    public async Task AiDraft_DoesNotApproveEvidenceOrAssignGrades()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Human review gate quiz",
            null,
            [microSkillId]);
        var submission = await SubmitAssessmentAsync(studentId, published.Id, "Student work.");

        var draft = await RequestDraftAsync(
            teacherId,
            published.Id,
            submission.Id,
            microSkillId);

        var audit = await LoadAuditLogAsync(draft.AuditLogId);

        Assert.NotNull(audit);
        Assert.Null(audit.EvidenceId);
        Assert.Null(audit.FinalApprovedAt);
        Assert.DoesNotContain("grade", draft.DraftFeedback, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<AiFeedbackAuditLog?> LoadAuditLogAsync(Guid auditLogId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AssessmentDbContext>();
        return await db.AiFeedbackAuditLogs.FindAsync(auditLogId);
    }

    private async Task<AiFeedbackDraftResponse> RequestDraftAsync(
        string teacherId,
        Guid assessmentId,
        Guid submissionId,
        Guid microSkillId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/assessments/{assessmentId}/submissions/{submissionId}/ai-feedback-draft",
            teacherId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new RequestAiFeedbackDraftRequest(microSkillId));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<AiFeedbackDraftResponse>()
            ?? throw new InvalidOperationException("Missing draft payload.");
    }

    private async Task FinalizeAuditAsync(
        Guid auditLogId,
        string teacherId,
        string teacherEditedFeedback,
        Guid evidenceId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/assessments/ai-feedback-audit/{auditLogId}/finalize",
            teacherId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new FinalizeAiFeedbackAuditRequest(
            teacherEditedFeedback,
            evidenceId));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<AssessmentResponse> PublishAssessmentAsync(
        string teacherId,
        Guid organisationId,
        Guid classId,
        string title,
        DateTimeOffset? dueAt,
        IReadOnlyList<Guid> microSkillIds)
    {
        using var createRequest = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/assessments",
            teacherId,
            TestJwt.TeacherRole);
        createRequest.Content = JsonContent.Create(new CreateAssessmentRequest(
            organisationId,
            classId,
            title,
            null,
            dueAt,
            [],
            microSkillIds));

        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var draft = await createResponse.Content.ReadFromJsonAsync<AssessmentResponse>()
            ?? throw new InvalidOperationException("Missing assessment payload.");

        return await SendAsAsync<AssessmentResponse>(
            HttpMethod.Post,
            $"/api/v1/assessments/{draft.Id}/publish",
            teacherId,
            TestJwt.TeacherRole);
    }

    private async Task<SubmissionResponse> SubmitAssessmentAsync(
        string studentId,
        Guid assessmentId,
        string responses)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/assessments/{assessmentId}/submissions",
            studentId,
            TestJwt.StudentRole);
        request.Content = JsonContent.Create(new SubmitAssessmentRequest(responses));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubmissionResponse>()
            ?? throw new InvalidOperationException("Missing submission payload.");
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, string role)
    {
        using var request = TestJwt.Authorized(method, url, userId, role);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Missing response payload.");
    }
}
