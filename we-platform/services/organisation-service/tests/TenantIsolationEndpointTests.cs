using System.Net;
using System.Net.Http.Json;
using OrganisationService.Application;

namespace OrganisationService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<OrganisationWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantIsolationEndpointTests(OrganisationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Admin_FromDifferentTenant_CannotGetOrganisationById()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var adminId = Guid.NewGuid().ToString();

        var created = await CreateOrganisationAsAdminAsync(adminId, tenantB, "Tenant B School", UniqueCode("TBS"));

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{created.Id}",
            adminId,
            tenantA,
            TestJwt.AdminRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotListYearLevels()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, tenantB, "Isolation School", UniqueCode("ISO"));
        await CreateYearLevelAsAdminAsync(adminId, org.Id, org.Id, "Year 7", 7);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/year-levels",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<OrganisationResponse> CreateOrganisationAsAdminAsync(
        string adminId,
        Guid tenantId,
        string name,
        string code)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/organisations", adminId, tenantId, TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateOrganisationRequest(name, code));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrganisationResponse>())!;
    }

    private async Task<YearLevelResponse> CreateYearLevelAsAdminAsync(
        string adminId,
        Guid tenantId,
        Guid organisationId,
        string name,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/year-levels",
            adminId,
            tenantId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateYearLevelRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<YearLevelResponse>())!;
    }

    private static string UniqueCode(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();
}
