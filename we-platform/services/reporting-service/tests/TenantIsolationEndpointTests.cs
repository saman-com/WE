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

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotGetReport()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var teacherId = Guid.NewGuid().ToString();
        var reportId = await SeedReportAsync(tenantB, teacherId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/reports/{reportId}",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SchoolLeader_FromDifferentTenant_CannotViewEffectivenessAnalytics()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var leaderId = Guid.NewGuid().ToString();
        _accessChecker.AllowSchoolLeader(leaderId, tenantB);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/analytics/organisations/{tenantB}/effectiveness",
            leaderId,
            tenantA,
            TestJwt.SchoolLeaderRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

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
