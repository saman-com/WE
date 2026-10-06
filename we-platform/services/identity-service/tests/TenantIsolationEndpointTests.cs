using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityService.Application.Auth;
using IdentityService.Domain;
using IdentityService.Infrastructure.Data;
using WePlatform.Tenancy;

namespace IdentityService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IdentityWebApplicationFactory _factory;

    public TenantIsolationEndpointTests(IdentityWebApplicationFactory factory)
    {
        _factory = factory;
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

    [Fact]
    public async Task ParentTeachers_AreLimitedToTheCallerTenant()
    {
        // Probe: identity-service:parent-teachers
        _factory.ParentTeachers.TeacherUserIds =
        [
            IdentityDataSeeder.TeacherUserId,
            IdentityDataSeeder.TeacherBUserId
        ];

        using var schoolA = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/parents/me/teachers",
            IdentityDataSeeder.ParentUserId,
            DefaultTenant.Id,
            PlatformRoles.Parent);
        var responseA = await _client.SendAsync(schoolA);
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        var teachersA = await responseA.Content.ReadFromJsonAsync<List<ParentTeacherResponse>>();
        Assert.NotNull(teachersA);
        Assert.Contains(teachersA, teacher => teacher.Id == IdentityDataSeeder.TeacherUserId);
        Assert.DoesNotContain(teachersA, teacher => teacher.Id == IdentityDataSeeder.TeacherBUserId);

        using var schoolB = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/parents/me/teachers",
            IdentityDataSeeder.ParentUserId,
            Guid.Parse(IdentityDataSeeder.SchoolBTenantId),
            PlatformRoles.Parent);
        var responseB = await _client.SendAsync(schoolB);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
        var teachersB = await responseB.Content.ReadFromJsonAsync<List<ParentTeacherResponse>>();
        Assert.NotNull(teachersB);
        Assert.Contains(teachersB, teacher => teacher.Id == IdentityDataSeeder.TeacherBUserId);
        Assert.DoesNotContain(teachersB, teacher => teacher.Id == IdentityDataSeeder.TeacherUserId);
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload?.AccessToken ?? throw new InvalidOperationException("Login response missing token.");
    }
}
