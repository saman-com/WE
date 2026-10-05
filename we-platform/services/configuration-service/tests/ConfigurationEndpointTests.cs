using System.Net;
using System.Net.Http.Json;
using ConfigurationService.Application;

namespace ConfigurationService.Tests;

public class ConfigurationEndpointTests : IClassFixture<ConfigurationWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ConfigurationEndpointTests(ConfigurationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnauthenticatedRequest_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/regional-configuration");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotUpdateRegionalConfiguration()
    {
        var tenantId = Guid.CreateVersion7();
        var teacherId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Put,
            "/api/v1/regional-configuration",
            teacherId,
            tenantId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(SampleRequest());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SchoolLeader_ConfiguresAndTeacher_ReadsTenantConfiguration()
    {
        var tenantId = Guid.CreateVersion7();
        var leaderId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var requestBody = SampleRequest();

        using (var updateRequest = TestJwt.Authorized(
                   HttpMethod.Put,
                   "/api/v1/regional-configuration",
                   leaderId,
                   tenantId,
                   TestJwt.SchoolLeaderRole))
        {
            updateRequest.Content = JsonContent.Create(requestBody);
            var updateResponse = await _client.SendAsync(updateRequest);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        }

        using var readRequest = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/regional-configuration",
            teacherId,
            tenantId,
            TestJwt.TeacherRole);
        var readResponse = await _client.SendAsync(readRequest);
        readResponse.EnsureSuccessStatusCode();

        var config = await readResponse.Content.ReadFromJsonAsync<RegionalConfigurationResponse>();
        Assert.NotNull(config);
        Assert.Equal(tenantId, config.TenantId);
        Assert.Equal("Letter Grades", config.GradingScale.Name);
        Assert.Equal("Term 1", config.AcademicCalendar.Terms[0].Name);
        Assert.Equal("Summer Break", config.AcademicCalendar.Holidays[0].Name);
        Assert.Equal("Formative", config.AssessmentModels[0].Category);
        Assert.Equal("term-summary", config.ReportingTemplates[0].Id);
        Assert.Equal("en-NZ", config.LocaleSettings.LanguageCode);
    }

    [Fact]
    public async Task SchoolAdmin_ConfiguresRegionalPolicies_ForOwnTenant()
    {
        var tenantId = Guid.CreateVersion7();
        var schoolAdminId = Guid.NewGuid().ToString();
        var requestBody = SampleRequest() with
        {
            LocaleSettings = new LocaleSettingsDto("ar-AE", "AE", "dd/MM/yyyy", "Asia/Dubai"),
            GradingScale = new GradingScaleDto(
                "Percentage",
                [new GradingLevelDto("Pass", 50m, 100m), new GradingLevelDto("Fail", 0m, 49.99m)])
        };

        using var updateRequest = TestJwt.Authorized(
            HttpMethod.Put,
            "/api/v1/regional-configuration",
            schoolAdminId,
            tenantId,
            TestJwt.AdminRole);
        updateRequest.Content = JsonContent.Create(requestBody);
        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var readRequest = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/regional-configuration",
            schoolAdminId,
            tenantId,
            TestJwt.AdminRole);
        var readResponse = await _client.SendAsync(readRequest);
        readResponse.EnsureSuccessStatusCode();

        var config = await readResponse.Content.ReadFromJsonAsync<RegionalConfigurationResponse>();
        Assert.NotNull(config);
        Assert.Equal(tenantId, config.TenantId);
        Assert.Equal("Percentage", config.GradingScale.Name);
        Assert.Equal("ar-AE", config.LocaleSettings.LanguageCode);
        Assert.Equal("Asia/Dubai", config.LocaleSettings.TimeZone);
        Assert.Equal(schoolAdminId, config.UpdatedByUserId);
    }

    private static UpdateRegionalConfigurationRequest SampleRequest() =>
        new(
            new AcademicCalendarDto(
                [new AcademicTermDto("Term 1", new DateOnly(2026, 2, 1), new DateOnly(2026, 4, 15))],
                [new HolidayDto("Summer Break", new DateOnly(2026, 12, 20))]),
            new GradingScaleDto(
                "Letter Grades",
                [new GradingLevelDto("A", 85m, 100m), new GradingLevelDto("B", 70m, 84.99m)]),
            [new AssessmentModelDto("Ongoing Checks", "Formative")],
            [new ReportingTemplateDto("term-summary", "Term Summary", "pdf")],
            new LocaleSettingsDto("en-NZ", "NZ", "dd/MM/yyyy", "Pacific/Auckland"));
}
