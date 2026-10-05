using System.Net;
using FederationService.Domain;

namespace FederationService.Tests;

public class RoleAccessEndpointTests : IClassFixture<FederationWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RoleAccessEndpointTests(FederationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public static TheoryData<string, HttpStatusCode> FederationSchoolsRoleCases => new()
    {
        { "Student", HttpStatusCode.Forbidden },
        { "Teacher", HttpStatusCode.Forbidden },
        { "Parent", HttpStatusCode.Forbidden },
        { "SchoolLeader", HttpStatusCode.Forbidden },
        { "SystemAdministrator", HttpStatusCode.Forbidden },
        { PlatformRoles.FederationAdmin, HttpStatusCode.OK },
        { "EducationAuthorityOfficer", HttpStatusCode.Forbidden }
    };

    [Theory]
    [MemberData(nameof(FederationSchoolsRoleCases))]
    public async Task ListSchools_RoleMatrix_ReturnsExpectedStatus(string role, HttpStatusCode expected)
    {
        var federationId = Guid.CreateVersion7();
        var userId = Guid.NewGuid().ToString();
        var tenantId = Guid.CreateVersion7();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/federation/schools");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            role == PlatformRoles.FederationAdmin
                ? TestJwt.CreateForFederationAdmin(userId, federationId)
                : TestJwt.Create(userId, null, tenantId, role));

        var response = await _client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
    }
}
