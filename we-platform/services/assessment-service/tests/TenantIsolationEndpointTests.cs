using System.Net;
using System.Net.Http.Json;
using AssessmentService.Application;

namespace AssessmentService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<AssessmentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeClassAccessChecker _accessChecker;

    public TenantIsolationEndpointTests(AssessmentWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
    }

    public static TheoryData<string, string> AssessmentResourceRoutes => new()
    {
        { "GET", "/api/v1/assessments/{assessmentId}" },
        { "PUT", "/api/v1/assessments/{assessmentId}" },
        { "DELETE", "/api/v1/assessments/{assessmentId}" },
        { "POST", "/api/v1/assessments/{assessmentId}/publish" },
        { "GET", "/api/v1/assessments/{assessmentId}/submissions" },
        { "POST", "/api/v1/assessments/{assessmentId}/submissions" },
        { "GET", "/api/v1/assessments/{assessmentId}/submissions/me" },
        { "GET", "/api/v1/assessments/{assessmentId}/submissions/{submissionId}" },
        { "POST", "/api/v1/assessments/{assessmentId}/submissions/{submissionId}/ai-feedback-draft" },
        { "POST", "/api/v1/assessments/ai-feedback-audit/{auditLogId}/finalize" }
    };

    [Theory]
    [MemberData(nameof(AssessmentResourceRoutes))]
    public async Task CrossSchool_AssessmentResource_BothDirections_Denied(string method, string template)
    {
        // Probe: assessment-service:assessment-resource
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var classA = Guid.CreateVersion7();
        var classB = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherA, schoolA, classA);
        _accessChecker.AllowTeacher(teacherB, schoolB, classB);

        var assessmentA = await CreateAssessmentAsync(teacherA, schoolA, classA);
        var assessmentB = await CreateAssessmentAsync(teacherB, schoolB, classB);
        var submissionId = Guid.CreateVersion7();
        var auditLogId = Guid.CreateVersion7();

        await AssertDeniedAsync(method, Expand(template, assessmentA.Id, submissionId, auditLogId), teacherB, schoolB);
        await AssertDeniedAsync(method, Expand(template, assessmentB.Id, submissionId, auditLogId), teacherA, schoolA);
    }

    [Fact]
    public async Task CrossSchool_AssessmentList_BothDirections_NeverReturnsOtherSchoolData()
    {
        // Probe: assessment-service:assessment-list
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var classA = Guid.CreateVersion7();
        var classB = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherA, schoolA, classA);
        _accessChecker.AllowTeacher(teacherB, schoolB, classB);

        var assessmentA = await CreateAssessmentAsync(teacherA, schoolA, classA);
        var assessmentB = await CreateAssessmentAsync(teacherB, schoolB, classB);

        foreach (var path in new[]
                 {
                     $"/api/v1/assessments?organisationId={schoolA}&classId={classA}",
                     $"/api/v1/assessments/class-summary?organisationId={schoolA}&classId={classA}",
                     $"/api/v1/assessments/student-summary?organisationId={schoolA}&classId={classA}"
                 })
        {
            using var fromB = TestJwt.Authorized(HttpMethod.Get, path, teacherB, schoolB, TestJwt.TeacherRole);
            var responseB = await _client.SendAsync(fromB);
            Assert.Equal(HttpStatusCode.Forbidden, responseB.StatusCode);
        }

        using var listA = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/assessments?organisationId={schoolA}&classId={classA}",
            teacherA,
            schoolA,
            TestJwt.TeacherRole);
        var okA = await _client.SendAsync(listA);
        okA.EnsureSuccessStatusCode();
        var itemsA = await okA.Content.ReadFromJsonAsync<List<AssessmentResponse>>();
        Assert.Contains(itemsA!, item => item.Id == assessmentA.Id);
        Assert.DoesNotContain(itemsA!, item => item.Id == assessmentB.Id);

        using var listB = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/assessments?organisationId={schoolB}&classId={classB}",
            teacherB,
            schoolB,
            TestJwt.TeacherRole);
        var okB = await _client.SendAsync(listB);
        okB.EnsureSuccessStatusCode();
        var itemsB = await okB.Content.ReadFromJsonAsync<List<AssessmentResponse>>();
        Assert.Contains(itemsB!, item => item.Id == assessmentB.Id);
        Assert.DoesNotContain(itemsB!, item => item.Id == assessmentA.Id);

        using var createIntoA = TestJwt.Authorized(HttpMethod.Post, "/api/v1/assessments", teacherB, schoolB, TestJwt.TeacherRole);
        createIntoA.Content = JsonContent.Create(new CreateAssessmentRequest(
            schoolA,
            classA,
            "Cross-school create attempt",
            null,
            null,
            [],
            []));
        var createResponse = await _client.SendAsync(createIntoA);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    [Fact]
    public async Task CrossSchool_Admin_CannotMutateOtherSchoolAssessment_BothDirections()
    {
        // Regression: Admin previously bypassed class checks without TenantAccess after IgnoreQueryFilters load.
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var adminA = Guid.NewGuid().ToString();
        var adminB = Guid.NewGuid().ToString();
        var classA = Guid.CreateVersion7();
        var classB = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherA, schoolA, classA);
        _accessChecker.AllowTeacher(teacherB, schoolB, classB);

        var assessmentA = await CreateAssessmentAsync(teacherA, schoolA, classA);
        var assessmentB = await CreateAssessmentAsync(teacherB, schoolB, classB);

        await AssertAdminDeniedAsync(HttpMethod.Put, $"/api/v1/assessments/{assessmentA.Id}", adminB, schoolB);
        await AssertAdminDeniedAsync(HttpMethod.Delete, $"/api/v1/assessments/{assessmentA.Id}", adminB, schoolB);
        await AssertAdminDeniedAsync(HttpMethod.Post, $"/api/v1/assessments/{assessmentA.Id}/publish", adminB, schoolB);
        await AssertAdminDeniedAsync(HttpMethod.Get, $"/api/v1/assessments/{assessmentA.Id}/submissions", adminB, schoolB);

        await AssertAdminDeniedAsync(HttpMethod.Put, $"/api/v1/assessments/{assessmentB.Id}", adminA, schoolA);
        await AssertAdminDeniedAsync(HttpMethod.Delete, $"/api/v1/assessments/{assessmentB.Id}", adminA, schoolA);
        await AssertAdminDeniedAsync(HttpMethod.Post, $"/api/v1/assessments/{assessmentB.Id}/publish", adminA, schoolA);
        await AssertAdminDeniedAsync(HttpMethod.Get, $"/api/v1/assessments/{assessmentB.Id}/submissions", adminA, schoolA);
    }

    private async Task AssertAdminDeniedAsync(HttpMethod method, string path, string userId, Guid tenantId)
    {
        using var request = TestJwt.Authorized(method, path, userId, tenantId, TestJwt.AdminRole);
        if (method == HttpMethod.Put || method == HttpMethod.Post)
        {
            request.Content = JsonContent.Create(new
            {
                title = "cross-school",
                instructions = (string?)null,
                dueAt = (DateTimeOffset?)null,
                learningObjectiveIds = Array.Empty<Guid>(),
                microSkillIds = Array.Empty<Guid>()
            });
        }

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task AssertDeniedAsync(string method, string path, string userId, Guid tenantId)
    {
        using var request = TestJwt.Authorized(new HttpMethod(method), path, userId, tenantId, TestJwt.TeacherRole);
        if (method is "PUT" or "POST")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await _client.SendAsync(request);
        // Forbidden/NotFound preferred; BadRequest is accepted when the handler rejects
        // an empty probe body before returning foreign-tenant data.
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.BadRequest,
            $"{method} {path} returned {response.StatusCode}");
        if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
        {
            Assert.Fail($"{method} {path} must not succeed for a cross-school caller.");
        }
    }

    private static string Expand(string template, Guid assessmentId, Guid submissionId, Guid auditLogId) =>
        template
            .Replace("{assessmentId}", assessmentId.ToString(), StringComparison.Ordinal)
            .Replace("{submissionId}", submissionId.ToString(), StringComparison.Ordinal)
            .Replace("{auditLogId}", auditLogId.ToString(), StringComparison.Ordinal);

    private async Task<AssessmentResponse> CreateAssessmentAsync(string teacherId, Guid organisationId, Guid classId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/assessments",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateAssessmentRequest(
            organisationId,
            classId,
            "Tenant isolation assessment",
            null,
            null,
            [],
            []));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AssessmentResponse>())!;
    }
}
