using System.Net;
using System.Net.Http.Json;
using CurriculumService.Application;

namespace CurriculumService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<CurriculumWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantIsolationEndpointTests(CurriculumWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotGetCurriculumById()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var teacherId = Guid.NewGuid().ToString();

        var created = await CreateCurriculumAsync(teacherId, tenantB, tenantB);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/curriculum/{created.Id}",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<CurriculumResponse> CreateCurriculumAsync(string userId, Guid tenantId, Guid organisationId)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/curriculum", userId, tenantId, TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateCurriculumRequest(
            organisationId,
            "Science",
            "2026",
            null));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CurriculumResponse>())!;
    }
}
