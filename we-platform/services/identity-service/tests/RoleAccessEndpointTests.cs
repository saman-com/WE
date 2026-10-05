using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityService.Application.Auth;
using IdentityService.Domain;
using IdentityService.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Tenancy;

namespace IdentityService.Tests;

public class RoleAccessEndpointTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IdentityWebApplicationFactory _factory;

    public RoleAccessEndpointTests(IdentityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public static TheoryData<string, string, string> MeRoleCases => new()
    {
        { IdentityDataSeeder.StudentUserId, PlatformRoles.Student, IdentityDataSeeder.StudentEmail },
        { IdentityDataSeeder.TeacherUserId, PlatformRoles.Teacher, IdentityDataSeeder.TeacherEmail },
        { IdentityDataSeeder.ParentUserId, PlatformRoles.Parent, IdentityDataSeeder.ParentEmail },
        { IdentityDataSeeder.SchoolLeaderUserId, PlatformRoles.SchoolLeader, IdentityDataSeeder.SchoolLeaderEmail },
        { IdentityDataSeeder.FederationAdminUserId, PlatformRoles.FederationAdmin, IdentityDataSeeder.FederationAdminEmail },
        { IdentityDataSeeder.AuthorityUserId, PlatformRoles.EducationAuthorityOfficer, IdentityDataSeeder.AuthorityEmail }
    };

    [Theory]
    [MemberData(nameof(MeRoleCases))]
    public async Task Me_WithRoleToken_ReturnsOk(string userId, string role, string email)
    {
        using var request = TestJwt.Authorized(HttpMethod.Get, "/api/v1/auth/me", userId, DefaultTenant.Id, role);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal(email, profile.Email);
        Assert.Contains(role, profile.Roles);
    }

    [Fact]
    public async Task Me_WithSystemAdministratorToken_ReturnsOk()
    {
        var adminId = await ResolveAdminUserIdAsync();
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/auth/me",
            adminId,
            DefaultTenant.Id,
            PlatformRoles.SystemAdministrator);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal(IdentityDataSeeder.AdminEmail, profile.Email);
        Assert.Contains(PlatformRoles.SystemAdministrator, profile.Roles);
    }

    public static TheoryData<string, HttpStatusCode> AdminRoleCases => new()
    {
        { PlatformRoles.Student, HttpStatusCode.Forbidden },
        { PlatformRoles.Teacher, HttpStatusCode.Forbidden },
        { PlatformRoles.Parent, HttpStatusCode.Forbidden },
        { PlatformRoles.SchoolLeader, HttpStatusCode.Forbidden },
        { PlatformRoles.SystemAdministrator, HttpStatusCode.OK },
        { PlatformRoles.FederationAdmin, HttpStatusCode.Forbidden },
        { PlatformRoles.EducationAuthorityOfficer, HttpStatusCode.Forbidden }
    };

    [Theory]
    [MemberData(nameof(AdminRoleCases))]
    public async Task Admin_RoleMatrix_ReturnsExpectedStatus(string role, HttpStatusCode expected)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/admin");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwt.Create(Guid.NewGuid().ToString(), DefaultTenant.Id, role));

        var response = await _client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
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
