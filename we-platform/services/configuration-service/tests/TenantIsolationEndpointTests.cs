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
    public async Task CrossSchool_RegionalConfig_BothDirections_NeverReturnsOtherSchoolData()
    {
        // Probe: configuration-service:regional-config
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var leaderA = Guid.NewGuid().ToString();
        var leaderB = Guid.NewGuid().ToString();
        var readerA = Guid.NewGuid().ToString();
        var readerB = Guid.NewGuid().ToString();

        await UpsertAsync(
            leaderA,
            schoolA,
            new UpdateRegionalConfigurationRequest(
                new AcademicCalendarDto([], []),
                new GradingScaleDto("School A Scale", [new GradingLevelDto("Pass", 50m, 100m)]),
                [],
                [],
                new LocaleSettingsDto("en-AU", "AU", "dd/MM/yyyy", "Australia/Sydney")));

        await UpsertAsync(
            leaderB,
            schoolB,
            new UpdateRegionalConfigurationRequest(
                new AcademicCalendarDto([], []),
                new GradingScaleDto("School B Scale", [new GradingLevelDto("Merit", 60m, 100m)]),
                [],
                [],
                new LocaleSettingsDto("en-GB", "GB", "dd/MM/yyyy", "Europe/London")));

        var configA = await ReadConfigAsync(readerA, schoolA);
        Assert.Equal("School A Scale", configA.GradingScale.Name);
        Assert.DoesNotContain("School B Scale", configA.GradingScale.Name);

        var configB = await ReadConfigAsync(readerB, schoolB);
        Assert.Equal("School B Scale", configB.GradingScale.Name);
        Assert.DoesNotContain("School A Scale", configB.GradingScale.Name);

        // Leader B updating under School B JWT must not alter School A's stored config.
        await UpsertAsync(
            leaderB,
            schoolB,
            new UpdateRegionalConfigurationRequest(
                new AcademicCalendarDto([], []),
                new GradingScaleDto("School B Scale Updated", [new GradingLevelDto("Merit", 60m, 100m)]),
                [],
                [],
                new LocaleSettingsDto("en-GB", "GB", "dd/MM/yyyy", "Europe/London")));

        var stillA = await ReadConfigAsync(readerA, schoolA);
        Assert.Equal("School A Scale", stillA.GradingScale.Name);
        var updatedB = await ReadConfigAsync(readerB, schoolB);
        Assert.Equal("School B Scale Updated", updatedB.GradingScale.Name);
    }

    private async Task<RegionalConfigurationResponse> ReadConfigAsync(string userId, Guid tenantId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/regional-configuration",
            userId,
            tenantId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegionalConfigurationResponse>())!;
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
