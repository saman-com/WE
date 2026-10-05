using System.Net;
using WePlatform.Tenancy;

namespace OrganisationService.Tests;

public class RoleAccessEndpointTests : IClassFixture<OrganisationWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RoleAccessEndpointTests(OrganisationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public static TheoryData<string, HttpStatusCode> OrganisationListRoleCases => new()
    {
        { "Student", HttpStatusCode.OK },
        { "Teacher", HttpStatusCode.OK },
        { "Parent", HttpStatusCode.Forbidden },
        { "SchoolLeader", HttpStatusCode.OK },
        { "SystemAdministrator", HttpStatusCode.OK },
        { "FederationAdmin", HttpStatusCode.Forbidden },
        { "EducationAuthorityOfficer", HttpStatusCode.Forbidden }
    };

    [Theory]
    [MemberData(nameof(OrganisationListRoleCases))]
    public async Task ListOrganisations_RoleMatrix_ReturnsExpectedStatus(string role, HttpStatusCode expected)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/organisations",
            Guid.NewGuid().ToString(),
            DefaultTenant.Id,
            role);

        var response = await _client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
    }
}
