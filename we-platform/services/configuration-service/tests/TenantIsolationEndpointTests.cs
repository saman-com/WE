using System.Net;
using System.Net.Http.Json;
using ConfigurationService.Application;

namespace ConfigurationService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<ConfigurationWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantIsolationEndpointTests(ConfigurationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DifferentTenants_MaintainSeparateRegionalConfigurations()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var leaderA = Guid.NewGuid().ToString();
        var leaderB = Guid.NewGuid().ToString();
        var teacherA = Guid.NewGuid().ToString();

        await UpsertAsync(
            leaderA,
            tenantA,
            new UpdateRegionalConfigurationRequest(
                new AcademicCalendarDto([], []),
                new GradingScaleDto("Tenant A Scale", [new GradingLevelDto("Pass", 50m, 100m)]),
                [],
                [],
                new LocaleSettingsDto("en-AU", "AU", "dd/MM/yyyy", "Australia/Sydney")));

        await UpsertAsync(
            leaderB,
            tenantB,
            new UpdateRegionalConfigurationRequest(
                new AcademicCalendarDto([], []),
                new GradingScaleDto("Tenant B Scale", [new GradingLevelDto("Merit", 60m, 100m)]),
                [],
                [],
                new LocaleSettingsDto("en-GB", "GB", "dd/MM/yyyy", "Europe/London")));

        using var readRequest = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/regional-configuration",
            teacherA,
            tenantA,
            TestJwt.TeacherRole);
        var readResponse = await _client.SendAsync(readRequest);
        readResponse.EnsureSuccessStatusCode();

        var config = await readResponse.Content.ReadFromJsonAsync<RegionalConfigurationResponse>();
        Assert.NotNull(config);
        Assert.Equal("Tenant A Scale", config.GradingScale.Name);
        Assert.Equal("en-AU", config.LocaleSettings.LanguageCode);
        Assert.NotEqual("Tenant B Scale", config.GradingScale.Name);
    }

    private async Task UpsertAsync(string userId, Guid tenantId, UpdateRegionalConfigurationRequest body)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Put,
            "/api/v1/regional-configuration",
            userId,
            tenantId,
            TestJwt.SchoolLeaderRole);
        request.Content = JsonContent.Create(body);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
