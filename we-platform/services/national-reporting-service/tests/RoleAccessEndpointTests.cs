using System.Net;

namespace NationalReportingService.Tests;

public class RoleAccessEndpointTests : IClassFixture<NationalReportingWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RoleAccessEndpointTests(NationalReportingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public static TheoryData<string, HttpStatusCode> PolicyDashboardTrendsRoleCases => new()
    {
        { "Student", HttpStatusCode.Forbidden },
        { "Teacher", HttpStatusCode.Forbidden },
        { "Parent", HttpStatusCode.Forbidden },
        { "SchoolLeader", HttpStatusCode.Forbidden },
        { "SystemAdministrator", HttpStatusCode.Forbidden },
        { "FederationAdmin", HttpStatusCode.Forbidden },
        { "EducationAuthorityOfficer", HttpStatusCode.OK }
    };

    [Theory]
    [MemberData(nameof(PolicyDashboardTrendsRoleCases))]
    public async Task PolicyDashboardTrends_RoleMatrix_ReturnsExpectedStatus(
        string role,
        HttpStatusCode expected)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/trends",
            Guid.NewGuid().ToString(),
            role);

        var response = await _client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
    }
}
