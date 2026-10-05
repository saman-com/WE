using System.Net;
using WePlatform.Tenancy;

namespace AssessmentService.Tests;

public class RoleAccessEndpointTests : IClassFixture<AssessmentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AssessmentWebApplicationFactory _factory;

    public RoleAccessEndpointTests(AssessmentWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public static TheoryData<string, HttpStatusCode> ClassSummaryRoleCases => new()
    {
        { "Student", HttpStatusCode.Forbidden },
        { "Teacher", HttpStatusCode.OK },
        { "Parent", HttpStatusCode.Forbidden },
        { "SchoolLeader", HttpStatusCode.OK },
        { "SystemAdministrator", HttpStatusCode.OK },
        { "FederationAdmin", HttpStatusCode.Forbidden },
        { "EducationAuthorityOfficer", HttpStatusCode.Forbidden }
    };

    [Theory]
    [MemberData(nameof(ClassSummaryRoleCases))]
    public async Task ClassSummary_RoleMatrix_ReturnsExpectedStatus(string role, HttpStatusCode expected)
    {
        var organisationId = DefaultTenant.Id;
        var classId = Guid.CreateVersion7();
        var userId = Guid.NewGuid().ToString();

        if (role == "Teacher")
        {
            _factory.AccessChecker.AllowTeacher(userId, organisationId, classId);
        }

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/assessments/class-summary?organisationId={organisationId}&classId={classId}",
            userId,
            organisationId,
            role);

        var response = await _client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
    }
}
