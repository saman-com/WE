using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AiGatewayService.Application;
using AiGatewayService.Domain;
using AiGatewayService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
    public async Task Admin_FromDifferentTenant_DoesNotSeeOtherTenantAuditLogs()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var adminId = Guid.NewGuid().ToString();
        await SeedAuditLogAsync(tenantB, "tenant-b-service");

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/ai/audit-logs?limit=100",
            adminId,
            tenantA,
            TestJwt.AdminRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<List<AiAuditLogResponse>>();
        Assert.Empty(logs!);
    }

    [Fact]
    public async Task PlatformService_WithoutTenantClaim_CannotCompleteRequest()
    {
        var serviceId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, serviceId),
            new(ClaimTypes.NameIdentifier, serviceId),
            new(ClaimTypes.Role, TestJwt.PlatformServiceRole)
        };
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("test-signing-key-at-least-32-chars-long"));
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "we-platform-identity-test",
            audience: "we-platform-test",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key,
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/ai/complete");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token));
        request.Content = JsonContent.Create(new AiCompletionRequest(
            "assessment-feedback",
            "1.0.0",
            "teacher-review",
            new Dictionary<string, string> { ["studentName"] = "Alex" }));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
