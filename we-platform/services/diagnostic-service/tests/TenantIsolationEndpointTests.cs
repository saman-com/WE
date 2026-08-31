using System.Net;
using DiagnosticService.Application;

namespace DiagnosticService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<DiagnosticWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;

    public TenantIsolationEndpointTests(DiagnosticWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
    }

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotListStudentDiagnosticsWithMismatchedOrganisation()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.Allow(teacherId, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentId}?organisationId={tenantB}",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
