using System.Net;
using System.Net.Http.Json;
using IdentityService.Application.Auth;
using IdentityService.Domain;
using IdentityService.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Tenancy;

namespace IdentityService.Tests;

public class AccountEndpointTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IdentityWebApplicationFactory _factory;

    public AccountEndpointTests(IdentityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Administrator_CanEditNameAndRoleWithinTheTenant()
    {
        var adminId = await ResolveAdminUserIdAsync();
        var created = await CreateUserAsync(adminId, PlatformRoles.Student, "Editable Student");

        using var request = Authorized(HttpMethod.Put, $"/api/v1/users/{created.Id}", adminId, DefaultTenant.Id);
        request.Content = JsonContent.Create(new { name = "Renamed Teacher", role = PlatformRoles.Teacher });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<DirectoryUserResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Renamed Teacher", updated.Name);
        Assert.Contains(PlatformRoles.Teacher, updated.Roles);
        Assert.DoesNotContain(PlatformRoles.Student, updated.Roles);

        var listed = await ListUsersAsync(adminId, DefaultTenant.Id);
        var match = Assert.Single(listed, user => user.Id == created.Id);
        Assert.Equal("Renamed Teacher", match.Name);
        Assert.Contains(PlatformRoles.Teacher, match.Roles);
    }

    [Fact]
    public async Task Administrator_CanDeactivateAndReactivateAnAccount()
    {
        var adminId = await ResolveAdminUserIdAsync();
        var email = $"inactive.{Guid.NewGuid():N}@school.local";
        var created = await CreateUserAsync(adminId, PlatformRoles.Teacher, "Toggle Teacher", email);

        using var deactivate = Authorized(
            HttpMethod.Post,
            $"/api/v1/users/{created.Id}/deactivate",
            adminId,
            DefaultTenant.Id);
        var deactivated = await _client.SendAsync(deactivate);
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        var inactive = await deactivated.Content.ReadFromJsonAsync<DirectoryUserResponse>();
        Assert.NotNull(inactive);
        Assert.False(inactive.Active);

        var blocked = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Password123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, blocked.StatusCode);
        var blockedError = await blocked.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("auth.account_inactive", blockedError?.Code);

        using var reactivate = Authorized(
            HttpMethod.Post,
            $"/api/v1/users/{created.Id}/reactivate",
            adminId,
            DefaultTenant.Id);
        var reactivated = await _client.SendAsync(reactivate);
        Assert.Equal(HttpStatusCode.OK, reactivated.StatusCode);
        var active = await reactivated.Content.ReadFromJsonAsync<DirectoryUserResponse>();
        Assert.NotNull(active);
        Assert.True(active.Active);

        var allowed = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Password123!"));
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Administrator_CanResetAPasswordWithinTheTenant()
    {
        var adminId = await ResolveAdminUserIdAsync();
        var email = $"reset.{Guid.NewGuid():N}@school.local";
        var created = await CreateUserAsync(adminId, PlatformRoles.Parent, "Reset Parent", email);

        using var request = Authorized(
            HttpMethod.Post,
            $"/api/v1/users/{created.Id}/password",
            adminId,
            DefaultTenant.Id);
        request.Content = JsonContent.Create(new { password = "NewPassword1" });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var oldPassword = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Password123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);

        var newPassword = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "NewPassword1"));
        Assert.Equal(HttpStatusCode.OK, newPassword.StatusCode);
    }

    [Fact]
    public async Task Administrator_CannotDeactivateOwnAccountOrRemoveOwnAdminRole()
    {
        var adminId = await ResolveAdminUserIdAsync();

        using var deactivate = Authorized(
            HttpMethod.Post,
            $"/api/v1/users/{adminId}/deactivate",
            adminId,
            DefaultTenant.Id);
        var deactivateResponse = await _client.SendAsync(deactivate);
        Assert.Equal(HttpStatusCode.BadRequest, deactivateResponse.StatusCode);
        var deactivateError = await deactivateResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("users.cannot_deactivate_self", deactivateError?.Code);

        var stillSignsIn = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(IdentityDataSeeder.AdminEmail, IdentityDataSeeder.AdminPassword));
        Assert.Equal(HttpStatusCode.OK, stillSignsIn.StatusCode);

        using var demote = Authorized(HttpMethod.Put, $"/api/v1/users/{adminId}", adminId, DefaultTenant.Id);
        demote.Content = JsonContent.Create(new { name = "Demo Admin", role = PlatformRoles.Teacher });
        var demoteResponse = await _client.SendAsync(demote);
        Assert.Equal(HttpStatusCode.BadRequest, demoteResponse.StatusCode);
        var demoteError = await demoteResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("users.cannot_remove_own_admin", demoteError?.Code);

        using var rename = Authorized(HttpMethod.Put, $"/api/v1/users/{adminId}", adminId, DefaultTenant.Id);
        rename.Content = JsonContent.Create(new { name = "Demo Admin Renamed", role = PlatformRoles.SystemAdministrator });
        var renameResponse = await _client.SendAsync(rename);
        Assert.Equal(HttpStatusCode.OK, renameResponse.StatusCode);
    }

    [Theory]
    [InlineData("PUT", "/api/v1/users/{id}")]
    [InlineData("POST", "/api/v1/users/{id}/deactivate")]
    [InlineData("POST", "/api/v1/users/{id}/reactivate")]
    [InlineData("POST", "/api/v1/users/{id}/password")]
    public async Task AccountActions_FromAnotherTenant_ReturnForbidden(string method, string pathTemplate)
    {
        // Probe: identity-service:user-account
        var adminId = await ResolveAdminUserIdAsync();
        var created = await CreateUserAsync(adminId, PlatformRoles.Student, "Isolated Student");
        var path = pathTemplate.Replace("{id}", created.Id, StringComparison.Ordinal);

        using var request = Authorized(
            new HttpMethod(method),
            path,
            Guid.NewGuid().ToString(),
            Guid.Parse(IdentityDataSeeder.SchoolBTenantId));
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new { name = "Other School", role = PlatformRoles.Teacher });
        }
        else if (path.EndsWith("/password", StringComparison.Ordinal))
        {
            request.Content = JsonContent.Create(new { password = "NewPassword1" });
        }

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AccountActions_ForATeacher_AreForbidden()
    {
        var adminId = await ResolveAdminUserIdAsync();
        var created = await CreateUserAsync(adminId, PlatformRoles.Student, "Protected Student");

        using var request = Authorized(
            HttpMethod.Post,
            $"/api/v1/users/{created.Id}/deactivate",
            IdentityDataSeeder.TeacherUserId,
            DefaultTenant.Id,
            PlatformRoles.Teacher);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpRequestMessage Authorized(
        HttpMethod method,
        string url,
        string userId,
        Guid tenantId,
        string role = PlatformRoles.SystemAdministrator) =>
        TestJwt.Authorized(method, url, userId, tenantId, role);

    private async Task<DirectoryUserResponse> CreateUserAsync(
        string adminId,
        string role,
        string name,
        string? email = null)
    {
        email ??= $"{role.ToLowerInvariant()}.{Guid.NewGuid():N}@school.local";
        using var request = Authorized(HttpMethod.Post, "/api/v1/users", adminId, DefaultTenant.Id);
        request.Content = JsonContent.Create(new CreateUserRequest(email, "Password123!", name, role));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DirectoryUserResponse>()
            ?? throw new InvalidOperationException("Missing created user.");
    }

    private async Task<List<DirectoryUserResponse>> ListUsersAsync(string adminId, Guid tenantId)
    {
        using var request = Authorized(HttpMethod.Get, "/api/v1/users", adminId, tenantId);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<DirectoryUserResponse>>() ?? [];
    }

    private async Task<string> ResolveAdminUserIdAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(IdentityDataSeeder.AdminEmail);
        Assert.NotNull(admin);
        return admin.Id;
    }
}
