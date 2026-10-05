using System.Net;
using System.Net.Http.Json;
using EvidenceService.Application;
using WePlatform.AspNetCore;

namespace EvidenceService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<EvidenceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeClassAccessChecker _accessChecker;

    public TenantIsolationEndpointTests(EvidenceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
    }

    public static TheoryData<string, string> EvidenceResourceRoutes => new()
    {
        { "GET", "/api/v1/evidence/{evidenceId}" },
        { "PUT", "/api/v1/evidence/{evidenceId}" },
        { "DELETE", "/api/v1/evidence/{evidenceId}" }
    };

    [Theory]
    [MemberData(nameof(EvidenceResourceRoutes))]
    public async Task CrossSchool_EvidenceResource_BothDirections_Denied(string method, string template)
    {
        // Probe: evidence-service:evidence-resource
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var classA = Guid.CreateVersion7();
        var classB = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherA, schoolA, classA);
        _accessChecker.AllowTeacher(teacherB, schoolB, classB);

        var evidenceA = await ApproveEvidenceAsync(teacherA, schoolA, classA);
        var evidenceB = await ApproveEvidenceAsync(teacherB, schoolB, classB);

        await AssertDeniedAsync(method, template.Replace("{evidenceId}", evidenceA.Id.ToString()), teacherB, schoolB);
        await AssertDeniedAsync(method, template.Replace("{evidenceId}", evidenceB.Id.ToString()), teacherA, schoolA);
    }

    [Fact]
    public async Task CrossSchool_EvidenceList_BothDirections_NeverReturnsOtherSchoolData()
    {
        // Probe: evidence-service:evidence-list
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var classA = Guid.CreateVersion7();
        var classB = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherA, schoolA, classA);
        _accessChecker.AllowTeacher(teacherB, schoolB, classB);

        var evidenceA = await ApproveEvidenceAsync(teacherA, schoolA, classA);
        var evidenceB = await ApproveEvidenceAsync(teacherB, schoolB, classB);

        foreach (var path in new[]
                 {
                     $"/api/v1/evidence?assessmentId={evidenceA.AssessmentId}",
                     $"/api/v1/evidence/class-summary?organisationId={schoolA}&classId={classA}",
                     $"/api/v1/evidence/student-feedback?organisationId={schoolA}&classId={classA}&studentUserId={evidenceA.StudentUserId}"
                 })
        {
            using var fromB = TestJwt.Authorized(HttpMethod.Get, path, teacherB, schoolB, TestJwt.TeacherRole);
            var response = await _client.SendAsync(fromB);
            Assert.True(
                response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.OK,
                path + " => " + response.StatusCode);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var body = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain(evidenceA.Id.ToString(), body, StringComparison.OrdinalIgnoreCase);
            }
        }

        using var createIntoA = TestJwt.Authorized(HttpMethod.Post, "/api/v1/evidence", teacherB, schoolB, TestJwt.TeacherRole);
        createIntoA.Content = JsonContent.Create(new ApproveEvidenceRequest(
            schoolA,
            classA,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            "Cross-school approve",
            [new MicroSkillMarkRequest(Guid.CreateVersion7(), 0.5m, "x")]));
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(createIntoA)).StatusCode);

        using var listA = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/evidence?assessmentId={evidenceA.AssessmentId}",
            teacherA,
            schoolA,
            TestJwt.TeacherRole);
        var ok = await _client.SendAsync(listA);
        ok.EnsureSuccessStatusCode();
        var listed = await ok.Content.ReadFromJsonAsync<PagedResponse<EvidenceResponse>>();
        Assert.Contains(listed!.Items, item => item.Id == evidenceA.Id);
        Assert.DoesNotContain(listed.Items, item => item.Id == evidenceB.Id);
    }

    private async Task AssertDeniedAsync(string method, string path, string userId, Guid tenantId)
    {
        using var request = TestJwt.Authorized(new HttpMethod(method), path, userId, tenantId, TestJwt.TeacherRole);
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"{method} {path} returned {response.StatusCode}");
    }

    private async Task<EvidenceResponse> ApproveEvidenceAsync(string teacherId, Guid organisationId, Guid classId)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/evidence", teacherId, organisationId, TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new ApproveEvidenceRequest(
            organisationId,
            classId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            "Cross-tenant evidence",
            [new MicroSkillMarkRequest(Guid.CreateVersion7(), 0.8m, "Good work")]));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EvidenceResponse>())!;
    }
}
