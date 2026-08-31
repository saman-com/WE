using System.Net;
using System.Net.Http.Json;
using FederationService.Application;
using FederationService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace FederationService.Tests;

public class FederationEndpointTests : IClassFixture<FederationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FederationWebApplicationFactory _factory;

    public FederationEndpointTests(FederationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task FederationAdmin_CreatesSchoolTenant_AndAssignsSchoolAdmin()
    {
        var federationId = Guid.CreateVersion7();
        var adminId = Guid.NewGuid().ToString();
        var schoolAdminUserId = Guid.NewGuid().ToString();
        var code = UniqueCode("SCH");

        using var createRequest = TestJwt.FederationAuthorized(
            HttpMethod.Post,
            "/api/v1/federation/schools",
            adminId,
            federationId);
        createRequest.Content = JsonContent.Create(new CreateFederationSchoolRequest("Northview High", code));

        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<FederationSchoolResponse>();
        Assert.NotNull(created);
        Assert.Equal("Northview High", created.Name);
        Assert.Equal(code, created.Code);
        Assert.Equal(federationId, created.FederationId);
        Assert.True(created.HasDefaultConfiguration);

        using var assignRequest = TestJwt.FederationAuthorized(
            HttpMethod.Post,
            $"/api/v1/federation/schools/{created.TenantId}/admins",
            adminId,
            federationId);
        assignRequest.Content = JsonContent.Create(new AssignSchoolAdminRequest(schoolAdminUserId));

        var assignResponse = await _client.SendAsync(assignRequest);
        Assert.Equal(HttpStatusCode.Created, assignResponse.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FederationDbContext>();
        var persistedAssignment = db.SchoolAdminAssignments
            .Single(item => item.SchoolTenantId == created.TenantId);
        Assert.Equal(schoolAdminUserId, persistedAssignment.UserId);
        Assert.Equal(federationId, persistedAssignment.FederationId);
    }

    [Fact]
    public async Task FederationAdmin_GetsAggregatedMetrics_WithoutStudentPii()
    {
        var federationId = Guid.CreateVersion7();
        var adminId = Guid.NewGuid().ToString();

        await SeedSchoolAsync(federationId, "Alpha School", "ALPHA", 120, 68.5m);
        await SeedSchoolAsync(federationId, "Beta School", "BETA", 80, 74.0m);

        using var request = TestJwt.FederationAuthorized(
            HttpMethod.Get,
            "/api/v1/federation/metrics",
            adminId,
            federationId);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var metrics = await response.Content.ReadFromJsonAsync<FederationMetricsResponse>();
        Assert.NotNull(metrics);
        Assert.Equal(2, metrics.TotalSchools);
        Assert.Equal(200, metrics.TotalEnrollment);
        Assert.Equal(2, metrics.Schools.Count);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("student", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", payload);
    }

    [Fact]
    public async Task SchoolAdmin_CannotAccessFederationEndpoints()
    {
        var tenantId = Guid.CreateVersion7();
        var schoolAdminId = Guid.NewGuid().ToString();

        using var listRequest = TestJwt.SchoolAdminAuthorized(
            HttpMethod.Get,
            "/api/v1/federation/schools",
            schoolAdminId,
            tenantId);
        var listResponse = await _client.SendAsync(listRequest);
        Assert.Equal(HttpStatusCode.Forbidden, listResponse.StatusCode);

        using var createRequest = TestJwt.SchoolAdminAuthorized(
            HttpMethod.Post,
            "/api/v1/federation/schools",
            schoolAdminId,
            tenantId);
        createRequest.Content = JsonContent.Create(new CreateFederationSchoolRequest("Blocked School", UniqueCode("BLK")));
        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    [Fact]
    public async Task SchoolProvisioning_CreatesDefaultRegionalConfiguration()
    {
        var federationId = Guid.CreateVersion7();
        var adminId = Guid.NewGuid().ToString();
        _factory.Provisioner.ProvisionedTenantIds.Clear();

        using var createRequest = TestJwt.FederationAuthorized(
            HttpMethod.Post,
            "/api/v1/federation/schools",
            adminId,
            federationId);
        createRequest.Content = JsonContent.Create(new CreateFederationSchoolRequest("Provisioned School", UniqueCode("PRV")));

        var createResponse = await _client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<FederationSchoolResponse>();
        Assert.NotNull(created);
        Assert.Contains(created.TenantId, _factory.Provisioner.ProvisionedTenantIds);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FederationDbContext>();
        var persisted = db.FederationSchools.Single(item => item.TenantId == created.TenantId);
        Assert.True(persisted.HasDefaultConfiguration);
    }

    [Fact]
    public async Task FederationAdmin_CannotAccessStudentPiiEndpoint()
    {
        var federationId = Guid.CreateVersion7();
        var adminId = Guid.NewGuid().ToString();
        var school = await SeedSchoolAsync(federationId, "Privacy School", "PRIV", 50, 60m);

        using var request = TestJwt.FederationAuthorized(
            HttpMethod.Get,
            $"/api/v1/federation/schools/{school.TenantId}/students",
            adminId,
            federationId);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FederationAdmin_FromDifferentFederation_CannotAssignSchoolAdmin()
    {
        var federationA = Guid.CreateVersion7();
        var federationB = Guid.CreateVersion7();
        var adminA = Guid.NewGuid().ToString();
        var schoolAdminUserId = Guid.NewGuid().ToString();
        var school = await SeedSchoolAsync(federationB, "Other Federation School", "OFS", 40, 55m);

        using var assignRequest = TestJwt.FederationAuthorized(
            HttpMethod.Post,
            $"/api/v1/federation/schools/{school.TenantId}/admins",
            adminA,
            federationA);
        assignRequest.Content = JsonContent.Create(new AssignSchoolAdminRequest(schoolAdminUserId));

        var assignResponse = await _client.SendAsync(assignRequest);

        Assert.Equal(HttpStatusCode.NotFound, assignResponse.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FederationDbContext>();
        Assert.DoesNotContain(
            db.SchoolAdminAssignments,
            item => item.SchoolTenantId == school.TenantId && item.UserId == schoolAdminUserId);
    }

    [Fact]
    public async Task FederationAdmin_FromDifferentFederation_CannotSeeOtherFederationMetrics()
    {
        var federationA = Guid.CreateVersion7();
        var federationB = Guid.CreateVersion7();
        var adminA = Guid.NewGuid().ToString();

        await SeedSchoolAsync(federationB, "Hidden School", "HID", 90, 72m);
        await SeedSchoolAsync(federationB, "Hidden School Two", "HI2", 60, 68m);

        using var request = TestJwt.FederationAuthorized(
            HttpMethod.Get,
            "/api/v1/federation/metrics",
            adminA,
            federationA);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var metrics = await response.Content.ReadFromJsonAsync<FederationMetricsResponse>();
        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.TotalSchools);
        Assert.Equal(0, metrics.TotalEnrollment);
    }

    [Fact]
    public async Task FederationAdmin_ProvisioningMultipleSchools_CompletesWithoutDegradation()
    {
        var federationId = Guid.CreateVersion7();
        var adminId = Guid.NewGuid().ToString();
        _factory.Provisioner.ProvisionedTenantIds.Clear();

        for (var index = 0; index < 5; index++)
        {
            using var createRequest = TestJwt.FederationAuthorized(
                HttpMethod.Post,
                "/api/v1/federation/schools",
                adminId,
                federationId);
            createRequest.Content = JsonContent.Create(
                new CreateFederationSchoolRequest($"School {index + 1}", UniqueCode($"S{index}")));

            var createResponse = await _client.SendAsync(createRequest);
            createResponse.EnsureSuccessStatusCode();
        }

        using var metricsRequest = TestJwt.FederationAuthorized(
            HttpMethod.Get,
            "/api/v1/federation/metrics",
            adminId,
            federationId);
        var metricsResponse = await _client.SendAsync(metricsRequest);
        metricsResponse.EnsureSuccessStatusCode();

        var metrics = await metricsResponse.Content.ReadFromJsonAsync<FederationMetricsResponse>();
        Assert.NotNull(metrics);
        Assert.Equal(5, metrics.TotalSchools);
        Assert.Equal(5, _factory.Provisioner.ProvisionedTenantIds.Count);
    }

    [Fact]
    public async Task FederationAdmin_ManagesFederationPolicies()
    {
        var federationId = Guid.CreateVersion7();
        var adminId = Guid.NewGuid().ToString();

        using var updateRequest = TestJwt.FederationAuthorized(
            HttpMethod.Put,
            "/api/v1/federation/policies",
            adminId,
            federationId);
        updateRequest.Content = JsonContent.Create(new UpdateFederationPoliciesRequest(
        [
            new FederationPolicyDto("shared-curriculum", "enabled"),
            new FederationPolicyDto("cross-school-reporting", "aggregated-only")
        ]));

        var updateResponse = await _client.SendAsync(updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        using var getRequest = TestJwt.FederationAuthorized(
            HttpMethod.Get,
            "/api/v1/federation/policies",
            adminId,
            federationId);
        var getResponse = await _client.SendAsync(getRequest);
        getResponse.EnsureSuccessStatusCode();

        var policies = await getResponse.Content.ReadFromJsonAsync<IReadOnlyList<FederationPolicyDto>>();
        Assert.NotNull(policies);
        Assert.Equal(2, policies.Count);
        Assert.Contains(policies, item => item.PolicyKey == "shared-curriculum" && item.PolicyValue == "enabled");
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
