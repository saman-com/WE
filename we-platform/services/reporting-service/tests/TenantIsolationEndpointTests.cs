using System.Net;
using System.Net.Http.Json;
using ReportingService.Application;
using ReportingService.Domain;
using ReportingService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ReportingService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<ReportingWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ReportingWebApplicationFactory _factory;
    private readonly FakeOrganisationAccessChecker _accessChecker;

    public TenantIsolationEndpointTests(ReportingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
        _accessChecker = factory.AccessChecker;
    }

    public static TheoryData<string, string> OrganisationPathRoutes => new()
    {
        { "GET", "/api/v1/analytics/organisations/{organisationId}/effectiveness" },
        { "GET", "/api/v1/analytics/organisations/{organisationId}/longitudinal" },
        {
            "GET",
            "/api/v1/analytics/organisations/{organisationId}/students/{studentUserId}/longitudinal"
        },
        { "POST", "/api/v1/reports/organisations/{organisationId}/school-summary" },
        { "POST", "/api/v1/reports/organisations/{organisationId}/classes/{classId}/class-progress" }
    };

    [Theory]
    [MemberData(nameof(OrganisationPathRoutes))]
    public async Task CrossSchool_OrganisationPath_BothDirections_Denied(string method, string template)
    {
        // Probe: reporting-service:organisation-path
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var leaderA = Guid.NewGuid().ToString();
        var leaderB = Guid.NewGuid().ToString();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var classA = Guid.CreateVersion7();
        var classB = Guid.CreateVersion7();

        _accessChecker.AllowSchoolLeader(leaderA, schoolA);
        _accessChecker.AllowSchoolLeader(leaderB, schoolB);
        _accessChecker.AllowTeacher(teacherA, schoolA, classA);
        _accessChecker.AllowTeacher(teacherB, schoolB, classB);
        _accessChecker.AllowTeacherForStudent(teacherA, studentA);

        await AssertDeniedAsync(
            method,
            Expand(template, schoolA, classA, studentA),
            leaderB,
            schoolB,
            TestJwt.SchoolLeaderRole);
        await AssertDeniedAsync(
            method,
            Expand(template, schoolB, classB, studentA),
            leaderA,
            schoolA,
            TestJwt.SchoolLeaderRole);
    }

    [Fact]
    public async Task CrossSchool_ReportResource_BothDirections_Denied()
    {
        // Probe: reporting-service:report-resource
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var reportA = await SeedReportAsync(schoolA, teacherA);
        var reportB = await SeedReportAsync(schoolB, teacherB);

        await AssertReportDeniedAsync($"/api/v1/reports/{reportA}", teacherB, schoolB);
        await AssertReportDeniedAsync($"/api/v1/reports/{reportB}", teacherA, schoolA);
        await AssertReportDeniedAsync($"/api/v1/reports/{reportA}/pdf", teacherB, schoolB);
        await AssertReportDeniedAsync($"/api/v1/reports/{reportB}/pdf", teacherA, schoolA);
    }

    private async Task AssertDeniedAsync(
        string method,
        string path,
        string userId,
        Guid tenantId,
        string role)
    {
        using var request = TestJwt.Authorized(new HttpMethod(method), path, userId, tenantId, role);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.BadRequest,
            $"{method} {path} returned {response.StatusCode}");
    }

    private async Task AssertReportDeniedAsync(string path, string userId, Guid tenantId)
    {
        using var request = TestJwt.Authorized(HttpMethod.Get, path, userId, tenantId, TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string Expand(string template, Guid organisationId, Guid classId, string studentUserId) =>
        template
            .Replace("{organisationId}", organisationId.ToString(), StringComparison.Ordinal)
            .Replace("{classId}", classId.ToString(), StringComparison.Ordinal)
            .Replace("{studentUserId}", studentUserId, StringComparison.Ordinal);

    private async Task<Guid> SeedReportAsync(Guid tenantId, string requestedByUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        var report = new GeneratedReport
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ReportType = ReportTypes.SchoolSummary,
            OrganisationId = tenantId,
            RequestedByUserId = requestedByUserId,
            ContentJson = "{}",
            GeneratedAt = DateTimeOffset.UtcNow
        };
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        return report.Id;
    }
}
