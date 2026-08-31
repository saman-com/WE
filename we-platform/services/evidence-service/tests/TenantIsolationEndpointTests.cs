using System.Net;
using System.Net.Http.Json;
using EvidenceService.Application;

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

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotGetEvidenceById()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var teacherId = Guid.NewGuid().ToString();
        var classId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, tenantB, classId);

        var created = await ApproveEvidenceAsync(teacherId, tenantB, tenantB, classId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/evidence/{created.Id}",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<EvidenceResponse> ApproveEvidenceAsync(
        string teacherId,
        Guid tenantId,
        Guid organisationId,
        Guid classId)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/evidence", teacherId, tenantId, TestJwt.TeacherRole);
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
