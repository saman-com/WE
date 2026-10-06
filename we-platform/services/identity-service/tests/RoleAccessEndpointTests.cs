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

    [Fact]
    public async Task Users_ForAdministrator_ReturnsNamesWithoutRequiringTheCallerToKnowIds()
    {
        var adminId = await ResolveAdminUserIdAsync();
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/users",
            adminId,
            DefaultTenant.Id,
            PlatformRoles.SystemAdministrator);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<DirectoryUserResponse>>();
        Assert.NotNull(users);
        Assert.Contains(users, user => user.Name == "Demo Teacher" && user.Email == IdentityDataSeeder.TeacherEmail);
    }

    [Fact]
    public async Task Users_ForAnotherTenant_OmitsTheDemoSchool()
    {
        var adminId = await ResolveAdminUserIdAsync();
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/users",
            adminId,
            Guid.CreateVersion7(),
            PlatformRoles.SystemAdministrator);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<DirectoryUserResponse>>();
        Assert.NotNull(users);
        Assert.DoesNotContain(users, user => user.Email == IdentityDataSeeder.TeacherEmail);
    }

    [Fact]
    public async Task CreateUser_ForEveryRole_SetsPasswordAndSignsIn()
    {
        var adminId = await ResolveAdminUserIdAsync();
        foreach (var role in PlatformRoles.All)
        {
            var email = $"{role.ToLowerInvariant()}.{Guid.NewGuid():N}@school.local";
            using var request = TestJwt.Authorized(
                HttpMethod.Post,
                "/api/v1/users",
                adminId,
                DefaultTenant.Id,
                PlatformRoles.SystemAdministrator);
            request.Content = JsonContent.Create(new CreateUserRequest(email, "Password123!", $"New {role}", role));

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<DirectoryUserResponse>();
            Assert.NotNull(created);
            Assert.Equal($"New {role}", created.Name);
            Assert.Equal(email, created.Email);
            Assert.Contains(role, created.Roles);

            var login = await _client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest(email, "Password123!"));
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        }
    }

    [Fact]
    public async Task CreateUser_WeakPassword_RejectsWithoutCreatingTheAccount()
    {
        var adminId = await ResolveAdminUserIdAsync();
        var email = $"weak.{Guid.NewGuid():N}@school.local";
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/users",
            adminId,
            DefaultTenant.Id,
            PlatformRoles.SystemAdministrator);
        request.Content = JsonContent.Create(new CreateUserRequest(email, "short", "Weak Password", PlatformRoles.Teacher));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("users.password_invalid", error?.Code);

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "short"));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ReturnsEmailTaken()
    {
        var adminId = await ResolveAdminUserIdAsync();
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/users",
            adminId,
            DefaultTenant.Id,
            PlatformRoles.SystemAdministrator);
        request.Content = JsonContent.Create(new CreateUserRequest(
            IdentityDataSeeder.TeacherEmail,
            "Password123!",
            "Another Teacher",
            PlatformRoles.Teacher));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("users.email_taken", error?.Code);
    }

    [Fact]
    public async Task CreateUser_FederationAdmin_JoinsTheCallerTenantFederation()
    {
        var adminId = await ResolveAdminUserIdAsync();
        var email = $"fed.{Guid.NewGuid():N}@school.local";
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/users",
            adminId,
            DefaultTenant.Id,
            PlatformRoles.SystemAdministrator);
        request.Content = JsonContent.Create(new CreateUserRequest(
            email,
            "Password123!",
            "New Federation Admin",
            PlatformRoles.FederationAdmin));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Password123!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        var federationClaim = jwt.Claims.Single(claim => claim.Type == FederationClaimTypes.FederationId);
        Assert.Equal(IdentityDataSeeder.DemoFederationId, federationClaim.Value);
    }

    [Fact]
    public async Task CreateUser_ForTeacher_IsForbidden()
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/users",
            IdentityDataSeeder.TeacherUserId,
            DefaultTenant.Id,
            PlatformRoles.Teacher);
        request.Content = JsonContent.Create(new CreateUserRequest(
            $"teacher-made.{Guid.NewGuid():N}@school.local",
            "Password123!",
            "Not Allowed",
            PlatformRoles.Student));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Users_ForTeacher_ReturnsNamesInTheCallerTenant()
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/users",
            IdentityDataSeeder.TeacherUserId,
            DefaultTenant.Id,
            PlatformRoles.Teacher);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<DirectoryUserResponse>>();
        Assert.NotNull(users);
        Assert.Contains(users, user => user.Id == IdentityDataSeeder.StudentUserId && user.Name == "Demo Student");
        Assert.DoesNotContain(users, user => user.Email == IdentityDataSeeder.TeacherBEmail);
    }

    [Theory]
    [InlineData(PlatformRoles.Student, IdentityDataSeeder.StudentUserId)]
    [InlineData(PlatformRoles.SchoolLeader, IdentityDataSeeder.SchoolLeaderUserId)]
    public async Task Users_ForSchoolRoles_ReturnsNamesInTheCallerTenant(string role, string userId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/users",
            userId,
            DefaultTenant.Id,
            role);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<DirectoryUserResponse>>();
        Assert.NotNull(users);
        Assert.Contains(users, user => user.Id == IdentityDataSeeder.StudentUserId && user.Name == "Demo Student");
        Assert.DoesNotContain(users, user => user.Email == IdentityDataSeeder.StudentBEmail);
    }

    [Fact]
    public async Task Users_ForParent_IsForbidden()
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/users",
            IdentityDataSeeder.ParentUserId,
            DefaultTenant.Id,
            PlatformRoles.Parent);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
