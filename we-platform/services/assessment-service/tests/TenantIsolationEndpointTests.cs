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

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotGetAssessmentById()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var teacherId = Guid.NewGuid().ToString();
        var classId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, tenantB, classId);

        var created = await CreateAssessmentAsync(teacherId, tenantB, tenantB, classId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/assessments/{created.Id}",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<AssessmentResponse> CreateAssessmentAsync(
        string teacherId,
        Guid tenantId,
        Guid organisationId,
        Guid classId)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/assessments", teacherId, tenantId, TestJwt.TeacherRole);
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
