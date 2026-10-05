using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EiService.Application;
using EiService.Domain;
using EiService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace EiService.Tests;

public class AiSummaryDraftEndpointTests : IClassFixture<EiWebApplicationFactory>
{
    private readonly EiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly FakeClassAccessChecker _accessChecker;
    private readonly FakeClassInsightsProvider _insightsProvider;
    private readonly FakeAiGatewayClient _aiGateway;
    private readonly FakeStudentEiDataClient _studentEiData;

    public AiSummaryDraftEndpointTests(EiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _insightsProvider = factory.InsightsProvider;
        _aiGateway = factory.AiGateway;
        _studentEiData = factory.StudentEiData;
    }

    [Fact]
    public async Task Teacher_CanRequestLessonSummaryDraft()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();
        var unitId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();

        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _insightsProvider.SetResponse(
            organisationId,
            classId,
            BuildSampleInsights(organisationId, classId, microSkillId, evidenceId));

        _aiGateway.Calls.Clear();
        var draft = await RequestLessonSummaryDraftAsync(
            teacherId,
            organisationId,
            classId,
            unitId);

        Assert.False(string.IsNullOrWhiteSpace(draft.DraftContent));
        Assert.Equal("lesson-summary", draft.PromptId);
        Assert.Equal("1.0.0", draft.PromptVersion);
        Assert.True(draft.IsAiAssistedDraft);
        Assert.NotEqual(Guid.Empty, draft.AuditLogId);
        Assert.Single(_aiGateway.Calls);
        Assert.Equal("lesson-summary", _aiGateway.Calls[0].PromptId);
    }

    [Fact]
    public async Task Teacher_CanRequestProgressReportDraft()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();

        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _studentEiData.SetSnapshot(
            studentId,
            new StudentEiSnapshot(
                studentId,
                [
                    new StudentMasterySnapshot(
                        microSkillId,
                        "Developing",
                        "Partial understanding shown in recent evidence.")
                ],
                [
                    new StudentGapSnapshot(
                        Guid.CreateVersion7(),
                        evidenceId,
                        microSkillId,
                        "Medium",
                        "Medium",
                        "Expected Proficient; demonstrated Developing.")
                ],
                [
                    new StudentDiagnosticSnapshot(
                        evidenceId,
                        microSkillId,
                        "Developing",
                        "Needs reinforcement.",
                        DateTimeOffset.UtcNow)
                ]));

        _aiGateway.Calls.Clear();
        var draft = await RequestProgressReportDraftAsync(
            teacherId,
            studentId,
            organisationId,
            classId);

        Assert.False(string.IsNullOrWhiteSpace(draft.DraftContent));
        Assert.Equal("progress-report-narrative", draft.PromptId);
        Assert.True(draft.IsAiAssistedDraft);
        Assert.NotEqual(Guid.Empty, draft.AuditLogId);
        Assert.Single(_aiGateway.Calls);
        Assert.Equal("progress-report-narrative", _aiGateway.Calls[0].PromptId);
    }

    [Fact]
    public async Task Student_CannotRequestSummaryDraft()
    {
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();
        var unitId = Guid.CreateVersion7();

        using var lessonRequest = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/ei/organisations/{organisationId}/classes/{classId}/lesson-summary-draft",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        lessonRequest.Content = JsonContent.Create(new RequestLessonSummaryDraftRequest(unitId));
        var lessonResponse = await _client.SendAsync(lessonRequest);

        using var reportRequest = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/ei/students/{studentId}/progress-report-draft",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        reportRequest.Content = JsonContent.Create(new RequestProgressReportDraftRequest(
            organisationId,
            classId));
        var reportResponse = await _client.SendAsync(reportRequest);

        Assert.Equal(HttpStatusCode.Forbidden, lessonResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reportResponse.StatusCode);
    }

    [Fact]
    public async Task SummaryDraft_RequiresTeacherApprovalBeforeExport()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();
        var unitId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();

        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _insightsProvider.SetResponse(
            organisationId,
            classId,
            BuildSampleInsights(organisationId, classId, microSkillId, evidenceId));

        var draft = await RequestLessonSummaryDraftAsync(
            teacherId,
            organisationId,
            classId,
            unitId);

        var auditBefore = await LoadAuditLogAsync(draft.AuditLogId);
        Assert.NotNull(auditBefore);
        Assert.Null(auditBefore.FinalApprovedAt);
        Assert.True(draft.IsAiAssistedDraft);

        var editedContent = "Teacher-edited lesson summary ready for parents.";
        var finalized = await FinalizeAuditAsync(draft.AuditLogId, teacherId, organisationId, editedContent);

        Assert.False(finalized.IsAiAssistedDraft);
        Assert.True(finalized.FinalApprovedAt > DateTimeOffset.MinValue);

        var auditAfter = await LoadAuditLogAsync(draft.AuditLogId);
        Assert.NotNull(auditAfter);
        Assert.Equal(editedContent, auditAfter.TeacherEditedContent);
        Assert.NotNull(auditAfter.FinalApprovedAt);
    }

    [Fact]
    public async Task ContextBuilder_IncludesSlpEvidenceOnly_NotFullStudentRecord()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();

        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _studentEiData.SetSnapshot(
            studentId,
            new StudentEiSnapshot(
                studentId,
                [new StudentMasterySnapshot(microSkillId, "Developing", "Evidence-based mastery note.")],
                [new StudentGapSnapshot(
                    Guid.CreateVersion7(),
                    evidenceId,
                    microSkillId,
                    "Low",
                    "Low",
                    "Gap tied to evidence.")],
                []));

        _aiGateway.Calls.Clear();
        await RequestProgressReportDraftAsync(teacherId, studentId, organisationId, classId);

        var variables = _aiGateway.Calls[0].Variables;
        var serialized = JsonSerializer.Serialize(variables);

        Assert.Contains("slpEvidenceContext", variables.Keys, StringComparer.Ordinal);
        Assert.Contains("evidence", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("enrollment", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("studentName", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LessonSummary_ContextScopesToClassInsights_NotFullStudentRecord()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();
        var unitId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();

        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _insightsProvider.SetResponse(
            organisationId,
            classId,
            BuildSampleInsights(organisationId, classId, microSkillId, evidenceId));

        _aiGateway.Calls.Clear();
        await RequestLessonSummaryDraftAsync(teacherId, organisationId, classId, unitId);

        var variables = _aiGateway.Calls[0].Variables;
        var serialized = JsonSerializer.Serialize(variables);

        Assert.Contains("classInsightsSummary", variables.Keys, StringComparer.Ordinal);
        Assert.Contains("unitId", variables.Keys, StringComparer.Ordinal);
        Assert.Contains("evidence", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("enrollment", serialized, StringComparison.OrdinalIgnoreCase);
    }

    private static ClassEiInsightsResponse BuildSampleInsights(
        Guid organisationId,
        Guid classId,
        Guid microSkillId,
        Guid evidenceId) =>
        new(
            organisationId,
            classId,
            [
                new ClassMicroSkillMasteryDistribution(
                    microSkillId,
                    new Dictionary<string, int> { ["Developing"] = 2 },
                    2,
                    "Class mastery summary.",
                    [evidenceId])
            ],
            [],
            [],
            []);

    private async Task<AiSummaryAuditLog?> LoadAuditLogAsync(Guid auditLogId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EiDbContext>();
        return await db.AiSummaryAuditLogs.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == auditLogId);
    }

    private async Task<AiSummaryDraftResponse> RequestLessonSummaryDraftAsync(
        string teacherId,
        Guid organisationId,
        Guid classId,
        Guid unitId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/ei/organisations/{organisationId}/classes/{classId}/lesson-summary-draft",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new RequestLessonSummaryDraftRequest(unitId));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<AiSummaryDraftResponse>()
            ?? throw new InvalidOperationException("Missing lesson summary draft payload.");
    }

    private async Task<AiSummaryDraftResponse> RequestProgressReportDraftAsync(
        string teacherId,
        string studentUserId,
        Guid organisationId,
        Guid classId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/ei/students/{studentUserId}/progress-report-draft",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new RequestProgressReportDraftRequest(
            organisationId,
            classId));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<AiSummaryDraftResponse>()
            ?? throw new InvalidOperationException("Missing progress report draft payload.");
    }

    private async Task<FinalizeAiSummaryAuditResponse> FinalizeAuditAsync(
        Guid auditLogId,
        string teacherId,
        Guid organisationId,
        string teacherEditedContent)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/ei/ai-summary-audit/{auditLogId}/finalize",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new FinalizeAiSummaryAuditRequest(teacherEditedContent));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<FinalizeAiSummaryAuditResponse>()
            ?? throw new InvalidOperationException("Missing finalize payload.");
    }
}
