using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AiGatewayService.Application;
using AiGatewayService.Domain;
using AiGatewayService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace AiGatewayService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<AiGatewayWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AiGatewayWebApplicationFactory _factory;

    public TenantIsolationEndpointTests(AiGatewayWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task CrossSchool_AiAuditLogs_BothDirections_NeverReturnsOtherSchoolData()
    {
        // Probe: ai-gateway-service:ai
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var adminA = Guid.NewGuid().ToString();
        var adminB = Guid.NewGuid().ToString();
        await SeedAuditLogAsync(schoolA, "tenant-a-service");
        await SeedAuditLogAsync(schoolB, "tenant-b-service");

        using var listA = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/ai/audit-logs?limit=100",
            adminA,
            schoolA,
            TestJwt.AdminRole);
        var okA = await _client.SendAsync(listA);
        okA.EnsureSuccessStatusCode();
        var logsA = await okA.Content.ReadFromJsonAsync<List<AiAuditLogResponse>>() ?? [];
        var onlyA = Assert.Single(logsA);
        Assert.Equal("tenant-a-service", onlyA.CallerUserId);

        using var listB = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/ai/audit-logs?limit=100",
            adminB,
            schoolB,
            TestJwt.AdminRole);
        var okB = await _client.SendAsync(listB);
        okB.EnsureSuccessStatusCode();
        var logsB = await okB.Content.ReadFromJsonAsync<List<AiAuditLogResponse>>() ?? [];
        var onlyB = Assert.Single(logsB);
        Assert.Equal("tenant-b-service", onlyB.CallerUserId);
    }

    [Fact]
    public async Task CrossSchool_AiComplete_RequiresTenantClaim()
    {
        // Probe: ai-gateway-service:ai
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var serviceA = Guid.NewGuid().ToString();
        var serviceB = Guid.NewGuid().ToString();

        using var withoutTenant = BuildPlatformServiceRequest(serviceB, includeTenant: false);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(withoutTenant)).StatusCode);

        using var withTenantA = BuildPlatformServiceRequest(serviceA, includeTenant: true, schoolA);
        var okA = await _client.SendAsync(withTenantA);
        okA.EnsureSuccessStatusCode();

        using var withTenantB = BuildPlatformServiceRequest(serviceB, includeTenant: true, schoolB);
        var okB = await _client.SendAsync(withTenantB);
        okB.EnsureSuccessStatusCode();

        using var listA = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/ai/audit-logs?limit=100",
            Guid.NewGuid().ToString(),
            schoolA,
            TestJwt.AdminRole);
        var logsA = await _client.SendAsync(listA);
        logsA.EnsureSuccessStatusCode();
        var payloadA = await logsA.Content.ReadFromJsonAsync<List<AiAuditLogResponse>>() ?? [];
        var only = Assert.Single(payloadA);
        Assert.Equal(serviceA, only.CallerUserId);
        Assert.DoesNotContain(payloadA, log => log.CallerUserId == serviceB);
    }

    private static HttpRequestMessage BuildPlatformServiceRequest(
        string serviceId,
        bool includeTenant,
        Guid? tenantId = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, serviceId),
            new(ClaimTypes.NameIdentifier, serviceId),
            new(ClaimTypes.Role, TestJwt.PlatformServiceRole)
        };
        if (includeTenant && tenantId.HasValue)
        {
            claims.Add(new Claim(WePlatform.Tenancy.TenantClaimTypes.TenantId, tenantId.Value.ToString()));
        }

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("test-signing-key-at-least-32-chars-long"));
        var token = new JwtSecurityToken(
            issuer: "we-platform-identity-test",
            audience: "we-platform-test",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key,
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/ai/complete");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(token));
        request.Content = JsonContent.Create(new AiCompletionRequest(
            "assessment-feedback",
            "1.0.0",
            "teacher-review",
            new Dictionary<string, string> { ["studentName"] = "Alex" }));
        return request;
    }

    private async Task SeedAuditLogAsync(Guid tenantId, string callerUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AiGatewayDbContext>();
        db.AiAuditLogs.Add(new AiAuditLog
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            CallerUserId = callerUserId,
            PromptId = "assessment-feedback",
            PromptVersion = "1.0.0",
            ContextScope = "teacher-review",
            PromptVariablesJson = "{}",
            AiResponse = "seeded",
            Outcome = AiAuditOutcomes.Success,
            ProviderName = "Mock",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
