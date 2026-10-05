using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityService.Application.Auth;
using IdentityService.Infrastructure.Data;
using WePlatform.Tenancy;

namespace IdentityService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantIsolationEndpointTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Me_FromDifferentTenant_ReturnsForbidden()
    {
        var token = await LoginAndGetTokenAsync(
            IdentityDataSeeder.TeacherEmail,
            IdentityDataSeeder.TeacherPassword);
        var handler = new JwtSecurityTokenHandler();
        var userId = handler.ReadJwtToken(token).Subject;
        var otherTenantId = Guid.CreateVersion7();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwt.Create(userId, otherTenantId, "Teacher"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_IncludesTenantIdClaim()
    {
        var token = await LoginAndGetTokenAsync(
            IdentityDataSeeder.TeacherEmail,
            IdentityDataSeeder.TeacherPassword);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var tenantClaim = jwt.Claims.FirstOrDefault(c => c.Type == TenantClaimTypes.TenantId);

        Assert.NotNull(tenantClaim);
        Assert.Equal(DefaultTenant.Id.ToString(), tenantClaim.Value);
    }

    [Fact]
    public async Task Login_FederationAdmin_IncludesFederationIdClaim()
    {
        var token = await LoginAndGetTokenAsync(
            IdentityDataSeeder.FederationAdminEmail,
            IdentityDataSeeder.FederationAdminPassword);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var federationClaim = jwt.Claims.FirstOrDefault(c => c.Type == FederationClaimTypes.FederationId);

        Assert.NotNull(federationClaim);
        Assert.Equal(IdentityDataSeeder.DemoFederationId, federationClaim.Value);
    }

    [Fact]
    public async Task Login_Teacher_OmitsFederationIdClaim()
    {
        var token = await LoginAndGetTokenAsync(
            IdentityDataSeeder.TeacherEmail,
            IdentityDataSeeder.TeacherPassword);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == FederationClaimTypes.FederationId);
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload?.AccessToken ?? throw new InvalidOperationException("Login response missing token.");
    }
}
