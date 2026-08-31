using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IdentityService.Application.Auth;
using IdentityService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Tests;

public class AuthEndpointTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IdentityWebApplicationFactory _factory;

    public AuthEndpointTests(IdentityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwt()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(IdentityDataSeeder.TeacherEmail, IdentityDataSeeder.TeacherPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.True(payload.ExpiresInSeconds > 0);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(IdentityDataSeeder.TeacherEmail, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUserAndRoles()
    {
        var token = await LoginAndGetTokenAsync(IdentityDataSeeder.TeacherEmail, IdentityDataSeeder.TeacherPassword);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal(IdentityDataSeeder.TeacherEmail, profile.Email);
        Assert.Contains("Teacher", profile.Roles);
    }

    [Fact]
    public async Task AdminEndpoint_WithoutAdminRole_ReturnsForbidden()
    {
        var token = await LoginAndGetTokenAsync(IdentityDataSeeder.TeacherEmail, IdentityDataSeeder.TeacherPassword);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/admin");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithAdminRole_ReturnsOk()
    {
        var token = await LoginAndGetTokenAsync(IdentityDataSeeder.AdminEmail, IdentityDataSeeder.AdminPassword);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/admin");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SeedData_ContainsAllPlatformRoles()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

        foreach (var role in Domain.PlatformRoles.All)
        {
            Assert.True(await roleManager.RoleExistsAsync(role), $"Missing seeded role: {role}");
        }
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload?.AccessToken ?? throw new InvalidOperationException("Login response missing token.");
    }
}
