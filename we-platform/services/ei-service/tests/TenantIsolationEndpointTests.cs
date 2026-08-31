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
    private readonly FakeAiGatewayClient _aiGateway;

    public TenantIsolationEndpointTests(EiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
        _accessChecker = factory.AccessChecker;
        _insightsProvider = factory.InsightsProvider;
        _aiGateway = factory.AiGateway;
    }

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotRequestLessonSummaryDraft()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var teacherId = Guid.NewGuid().ToString();
        var classId = Guid.CreateVersion7();
        var unitId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();

        _accessChecker.AllowTeacher(teacherId, tenantB, classId);
        _insightsProvider.SetResponse(
            tenantB,
            classId,
            BuildSampleInsights(tenantB, classId, microSkillId, evidenceId));

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/ei/organisations/{tenantB}/classes/{classId}/lesson-summary-draft",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new RequestLessonSummaryDraftRequest(unitId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotFinalizeAuditLog()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var teacherId = Guid.NewGuid().ToString();
        var auditLogId = await SeedAuditLogAsync(tenantB, teacherId);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/ei/ai-summary-audit/{auditLogId}/finalize",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new FinalizeAiSummaryAuditRequest("Edited summary content."));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

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
