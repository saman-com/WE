using System.Net;
using System.Net.Http.Json;
using EiService.Application;
using EiService.Domain;
using EiService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Tenancy;

namespace EiService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<EiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly EiWebApplicationFactory _factory;
    private readonly FakeClassAccessChecker _accessChecker;
    private readonly FakeClassInsightsProvider _insightsProvider;

    public TenantIsolationEndpointTests(EiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
        _accessChecker = factory.AccessChecker;
        _insightsProvider = factory.InsightsProvider;
    }

    public static TheoryData<string, string> OrganisationPathRoutes => new()
    {
        { "GET", "/api/v1/ei/organisations/{organisationId}/classes/{classId}/insights" },
        { "POST", "/api/v1/ei/organisations/{organisationId}/classes/{classId}/lesson-summary-draft" }
    };

    [Theory]
    [MemberData(nameof(OrganisationPathRoutes))]
    public async Task CrossSchool_OrganisationPath_BothDirections_Denied(string method, string template)
    {
        // Probe: ei-service:organisation-path
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var classA = Guid.CreateVersion7();
        var classB = Guid.CreateVersion7();
        var unitId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();

        _accessChecker.AllowTeacher(teacherA, schoolA, classA);
        _accessChecker.AllowTeacher(teacherB, schoolB, classB);
        _insightsProvider.SetResponse(schoolA, classA, BuildSampleInsights(schoolA, classA, microSkillId, evidenceId));
        _insightsProvider.SetResponse(schoolB, classB, BuildSampleInsights(schoolB, classB, microSkillId, evidenceId));

        await AssertDeniedAsync(
            method,
            Expand(template, schoolA, classA),
            teacherB,
            schoolB,
            unitId);
        await AssertDeniedAsync(
            method,
            Expand(template, schoolB, classB),
            teacherA,
            schoolA,
            unitId);
    }

    [Fact]
    public async Task CrossSchool_PersonResource_BothDirections_Denied()
    {
        // Probe: ei-service:person-resource
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var studentB = Guid.NewGuid().ToString();
        var classA = Guid.CreateVersion7();
        var classB = Guid.CreateVersion7();

        _accessChecker.AllowTeacher(teacherA, schoolA, classA);
        _accessChecker.AllowTeacher(teacherB, schoolB, classB);

        await AssertProgressDraftDeniedAsync(teacherB, schoolB, studentA, schoolA, classA);
        await AssertProgressDraftDeniedAsync(teacherA, schoolA, studentB, schoolB, classB);
    }

    [Fact]
    public async Task CrossSchool_AuditResource_BothDirections_Denied()
    {
        // Probe: ei-service:audit-resource
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var auditA = await SeedAuditLogAsync(schoolA, teacherA);
        var auditB = await SeedAuditLogAsync(schoolB, teacherB);

        await AssertFinalizeDeniedAsync(teacherB, schoolB, auditA);
        await AssertFinalizeDeniedAsync(teacherA, schoolA, auditB);
    }

    private async Task AssertDeniedAsync(
        string method,
        string path,
        string userId,
        Guid tenantId,
        Guid unitId)
    {
        using var request = TestJwt.Authorized(new HttpMethod(method), path, userId, tenantId, TestJwt.TeacherRole);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new RequestLessonSummaryDraftRequest(unitId));
        }

        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.BadRequest,
            $"{method} {path} returned {response.StatusCode}");
    }

    private async Task AssertProgressDraftDeniedAsync(
        string callerId,
        Guid callerTenant,
        string studentUserId,
        Guid targetOrganisationId,
        Guid targetClassId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/ei/students/{studentUserId}/progress-report-draft",
            callerId,
            callerTenant,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new RequestProgressReportDraftRequest(targetOrganisationId, targetClassId));
        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.BadRequest,
            $"progress-report-draft returned {response.StatusCode}");
    }

    private async Task AssertFinalizeDeniedAsync(string callerId, Guid callerTenant, Guid auditLogId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/ei/ai-summary-audit/{auditLogId}/finalize",
            callerId,
            callerTenant,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new FinalizeAiSummaryAuditRequest("Edited summary content."));
        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"finalize audit returned {response.StatusCode}");
    }

    private static string Expand(string template, Guid organisationId, Guid classId) =>
        template
            .Replace("{organisationId}", organisationId.ToString(), StringComparison.Ordinal)
            .Replace("{classId}", classId.ToString(), StringComparison.Ordinal);

    private async Task<Guid> SeedAuditLogAsync(Guid tenantId, string teacherUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EiDbContext>();
        var auditLog = new AiSummaryAuditLog
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            SummaryType = AiSummaryTypes.LessonSummary,
            OrganisationId = tenantId,
            ClassId = Guid.CreateVersion7(),
            TeacherUserId = teacherUserId,
            PromptId = "lesson-summary",
            PromptVersion = "1.0.0",
            PromptVariablesJson = "{}",
            ContextScopeJson = "{}",
            AiResponse = "Draft",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.AiSummaryAuditLogs.Add(auditLog);
        await db.SaveChangesAsync();
        return auditLog.Id;
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
                    new Dictionary<string, int> { ["Mastered"] = 1 },
                    1,
                    "student-1: Strong mastery.",
                    [evidenceId])
            ],
            [],
            [],
            []);
}
