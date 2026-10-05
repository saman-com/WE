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

    [Fact]
    public async Task CrossFederation_ListCreatePoliciesMetrics_BothDirections_Isolated()
    {
        var federationA = Guid.CreateVersion7();
        var federationB = Guid.CreateVersion7();
        var adminA = Guid.NewGuid().ToString();
        var adminB = Guid.NewGuid().ToString();

        var schoolInA = await SeedSchoolAsync(federationA, "Alpha Fed A", UniqueCode("AFA"), 30, 61m);
        var schoolInB = await SeedSchoolAsync(federationB, "Beta Fed B", UniqueCode("BFB"), 40, 71m);

        await SeedPolicyAsync(federationA, "shared-curriculum", "enabled-a");
        await SeedPolicyAsync(federationB, "shared-curriculum", "enabled-b");

        // List schools: each admin only sees own federation
        var listA = await ListSchoolsAsync(adminA, federationA);
        Assert.Contains(listA, school => school.TenantId == schoolInA.TenantId);
        Assert.DoesNotContain(listA, school => school.TenantId == schoolInB.TenantId);

        var listB = await ListSchoolsAsync(adminB, federationB);
        Assert.Contains(listB, school => school.TenantId == schoolInB.TenantId);
        Assert.DoesNotContain(listB, school => school.TenantId == schoolInA.TenantId);

        // Create school: A cannot create into B's namespace (creates under caller's federation claim)
        var createIntoBAsA = await CreateSchoolAsync(adminA, federationA, "Should Stay In A", UniqueCode("SIA"));
        Assert.Equal(HttpStatusCode.Created, createIntoBAsA.StatusCode);
        var createdByA = await createIntoBAsA.Content.ReadFromJsonAsync<FederationSchoolResponse>();
        Assert.NotNull(createdByA);
        Assert.Equal(federationA, createdByA.FederationId);

        // Policies: A cannot read or overwrite B
        var policiesSeenByA = await ListPoliciesAsync(adminA, federationA);
        Assert.Contains(policiesSeenByA, p => p.PolicyKey == "shared-curriculum" && p.PolicyValue == "enabled-a");
        Assert.DoesNotContain(policiesSeenByA, p => p.PolicyValue == "enabled-b");

        var policiesSeenByB = await ListPoliciesAsync(adminB, federationB);
        Assert.Contains(policiesSeenByB, p => p.PolicyKey == "shared-curriculum" && p.PolicyValue == "enabled-b");
        Assert.DoesNotContain(policiesSeenByB, p => p.PolicyValue == "enabled-a");

        using var overwriteB = TestJwt.FederationAuthorized(
            HttpMethod.Put,
            "/api/v1/federation/policies",
            adminA,
            federationA);
        overwriteB.Content = JsonContent.Create(new UpdateFederationPoliciesRequest(
        [
            new FederationPolicyDto("shared-curriculum", "hijacked-by-a")
        ]));
        var overwriteResponse = await _client.SendAsync(overwriteB);
        overwriteResponse.EnsureSuccessStatusCode();

        var policiesBAfter = await ListPoliciesAsync(adminB, federationB);
        Assert.Contains(policiesBAfter, p => p.PolicyKey == "shared-curriculum" && p.PolicyValue == "enabled-b");
        Assert.DoesNotContain(policiesBAfter, p => p.PolicyValue == "hijacked-by-a");

        // Metrics both directions
        var metricsA = await GetMetricsAsync(adminA, federationA);
        Assert.True(metricsA.TotalSchools >= 2);
        Assert.DoesNotContain(metricsA.Schools, s => s.TenantId == schoolInB.TenantId);

        var metricsB = await GetMetricsAsync(adminB, federationB);
        Assert.True(metricsB.TotalSchools >= 1);
        Assert.DoesNotContain(metricsB.Schools, s => s.TenantId == schoolInA.TenantId);
        Assert.DoesNotContain(metricsB.Schools, s => s.TenantId == createdByA.TenantId);
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

    private async Task<IReadOnlyList<FederationSchoolResponse>> ListSchoolsAsync(string adminId, Guid federationId)
    {
        using var request = TestJwt.FederationAuthorized(
            HttpMethod.Get,
            "/api/v1/federation/schools",
            adminId,
            federationId);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<FederationSchoolResponse>>())!;
    }

    private async Task<HttpResponseMessage> CreateSchoolAsync(
        string adminId,
        Guid federationId,
        string name,
        string code)
    {
        using var request = TestJwt.FederationAuthorized(
            HttpMethod.Post,
            "/api/v1/federation/schools",
            adminId,
            federationId);
        request.Content = JsonContent.Create(new CreateFederationSchoolRequest(name, code));
        return await _client.SendAsync(request);
    }

    private async Task<IReadOnlyList<FederationPolicyDto>> ListPoliciesAsync(string adminId, Guid federationId)
    {
        using var request = TestJwt.FederationAuthorized(
            HttpMethod.Get,
            "/api/v1/federation/policies",
            adminId,
            federationId);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<FederationPolicyDto>>())!;
    }

    private async Task<FederationMetricsResponse> GetMetricsAsync(string adminId, Guid federationId)
    {
        using var request = TestJwt.FederationAuthorized(
            HttpMethod.Get,
            "/api/v1/federation/metrics",
            adminId,
            federationId);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FederationMetricsResponse>())!;
    }

    private async Task SeedPolicyAsync(Guid federationId, string key, string value)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FederationDbContext>();
        db.FederationPolicies.Add(new Domain.FederationPolicy
        {
            Id = Guid.CreateVersion7(),
            FederationId = federationId,
            PolicyKey = key,
            PolicyValue = value,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
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

    private static string UniqueCode(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();
}
