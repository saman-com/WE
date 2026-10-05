using System.Net;
using System.Net.Http.Json;
using ConfigurationService.Application;

namespace ConfigurationService.Tests;

public class RoleAccessEndpointTests : IClassFixture<ConfigurationWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RoleAccessEndpointTests(ConfigurationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public static TheoryData<string, HttpStatusCode> UpdateRegionalConfigurationRoleCases => new()
    {
        { "Student", HttpStatusCode.Forbidden },
        { "Teacher", HttpStatusCode.Forbidden },
        { "Parent", HttpStatusCode.Forbidden },
        { "SchoolLeader", HttpStatusCode.OK },
        { "SystemAdministrator", HttpStatusCode.OK },
        { "FederationAdmin", HttpStatusCode.Forbidden },
        { "EducationAuthorityOfficer", HttpStatusCode.Forbidden }
    };

    [Theory]
    [MemberData(nameof(UpdateRegionalConfigurationRoleCases))]
    public async Task UpdateRegionalConfiguration_RoleMatrix_ReturnsExpectedStatus(
        string role,
        HttpStatusCode expected)
    {
        var tenantId = Guid.CreateVersion7();
        using var request = TestJwt.Authorized(
            HttpMethod.Put,
            "/api/v1/regional-configuration",
            Guid.NewGuid().ToString(),
            tenantId,
            role);
        request.Content = JsonContent.Create(SampleRequest());

        var response = await _client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
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
