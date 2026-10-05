using System.Net;
using System.Net.Http.Json;
using FederationService.Application;
using FederationService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace FederationService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<FederationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FederationWebApplicationFactory _factory;

    public TenantIsolationEndpointTests(FederationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task CrossFederation_SchoolAdminAssign_BothDirections_Denied()
    {
        // Probe: federation-service:federation-school
        var federationA = Guid.CreateVersion7();
        var federationB = Guid.CreateVersion7();
        var adminA = Guid.NewGuid().ToString();
        var adminB = Guid.NewGuid().ToString();
        var schoolAdminUserId = Guid.NewGuid().ToString();

        var schoolInA = await SeedSchoolAsync(federationA, "School Federation A", "SFA", 10, 50m);
        var schoolInB = await SeedSchoolAsync(federationB, "School Federation B", "SFB", 20, 60m);

        await AssertAssignDeniedAsync(adminB, federationB, schoolInA.TenantId, schoolAdminUserId);
        await AssertAssignDeniedAsync(adminA, federationA, schoolInB.TenantId, schoolAdminUserId);
    }

    private async Task AssertAssignDeniedAsync(
        string federationAdminId,
        Guid federationId,
        Guid schoolTenantId,
        string schoolAdminUserId)
    {
        using var assignRequest = TestJwt.FederationAuthorized(
            HttpMethod.Post,
            $"/api/v1/federation/schools/{schoolTenantId}/admins",
            federationAdminId,
            federationId);
        assignRequest.Content = JsonContent.Create(new AssignSchoolAdminRequest(schoolAdminUserId));

        var assignResponse = await _client.SendAsync(assignRequest);
        Assert.True(
            assignResponse.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden,
            $"assign admin returned {assignResponse.StatusCode}");

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FederationDbContext>();
        Assert.DoesNotContain(
            db.SchoolAdminAssignments,
            item => item.SchoolTenantId == schoolTenantId && item.UserId == schoolAdminUserId);
    }

    private async Task<FederationSchoolResponse> SeedSchoolAsync(
        Guid federationId,
        string name,
        string code,
        int enrollment,
        decimal progress)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FederationDbContext>();
        var tenantId = Guid.CreateVersion7();
        db.FederationSchools.Add(new Domain.FederationSchool
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            FederationId = federationId,
            Name = name,
            Code = code,
            EnrollmentCount = enrollment,
            AverageProgressPercent = progress,
            HasDefaultConfiguration = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return new FederationSchoolResponse(tenantId, federationId, name, code, true, DateTimeOffset.UtcNow);
    }
}
