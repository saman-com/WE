using System.Net;
using System.Net.Http.Json;
using InterventionService.Application;

namespace InterventionService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<InterventionWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;

    public TenantIsolationEndpointTests(InterventionWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
    }

    [Fact]
    public async Task SchoolLeader_FromDifferentTenant_CannotListOrganisationInterventions()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var leaderId = Guid.NewGuid().ToString();
        _accessChecker.AllowSchoolLeaderForOrganisation(leaderId, tenantB);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{tenantB}/interventions",
            leaderId,
            tenantA,
            TestJwt.SchoolLeaderRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotGetInterventionById()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherId, studentId);

        var created = await SendAsAsync<InterventionResponse>(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            tenantB,
            TestJwt.TeacherRole,
            new CreateInterventionRequest(
                tenantB,
                studentId,
                Guid.CreateVersion7(),
                "Cross-tenant isolation test.",
                null,
                null,
                null,
                null));

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/interventions/{created.Id}",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<TResponse> SendAsAsync<TResponse>(
        HttpMethod method,
        string url,
        string userId,
        Guid tenantId,
        string role,
        object? body = null)
    {
        using var request = TestJwt.Authorized(method, url, userId, tenantId, role);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }
}
