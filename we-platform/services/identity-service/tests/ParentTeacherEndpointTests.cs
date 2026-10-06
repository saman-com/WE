using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IdentityService.Application.Auth;
using IdentityService.Domain;
using IdentityService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Tenancy;

namespace IdentityService.Tests;

public class ParentTeacherEndpointTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IdentityWebApplicationFactory _factory;

    public ParentTeacherEndpointTests(IdentityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Parent_GetsTheirChildsTeacher_WithIdAndNameOnly()
    {
        _factory.ParentTeachers.TeacherUserIds = [IdentityDataSeeder.TeacherUserId];

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/parents/me/teachers",
            IdentityDataSeeder.ParentUserId,
            DefaultTenant.Id,
            PlatformRoles.Parent);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var teachers = document.RootElement.EnumerateArray().ToList();
        Assert.Single(teachers);
        var teacher = teachers[0];
        Assert.Equal(2, teacher.EnumerateObject().Count());
        Assert.Equal(IdentityDataSeeder.TeacherUserId, teacher.GetProperty("id").GetString());
        Assert.Equal("Demo Teacher", teacher.GetProperty("name").GetString());
        Assert.DoesNotContain(IdentityDataSeeder.TeacherEmail, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("roles", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Parent_DoesNotGetOtherTeachersStudentsOrOtherSchools()
    {
        var adminId = await ResolveAdminUserIdAsync();
        var otherEmail = $"other.{Guid.NewGuid():N}@school.local";
        using (var create = TestJwt.Authorized(
                   HttpMethod.Post,
                   "/api/v1/users",
                   adminId,
                   DefaultTenant.Id,
                   PlatformRoles.SystemAdministrator))
        {
            create.Content = JsonContent.Create(new CreateUserRequest(
                otherEmail,
                "Password123!",
                "Other Teacher",
                PlatformRoles.Teacher));
            var created = await _client.SendAsync(create);
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        }

        _factory.ParentTeachers.TeacherUserIds =
        [
            IdentityDataSeeder.TeacherUserId,
            IdentityDataSeeder.StudentUserId,
            IdentityDataSeeder.TeacherBUserId
        ];

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/parents/me/teachers",
            IdentityDataSeeder.ParentUserId,
            DefaultTenant.Id,
            PlatformRoles.Parent);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var teachers = await response.Content.ReadFromJsonAsync<List<ParentTeacherResponse>>();
        Assert.NotNull(teachers);
        Assert.Contains(teachers, teacher => teacher.Id == IdentityDataSeeder.TeacherUserId && teacher.Name == "Demo Teacher");
        Assert.DoesNotContain(teachers, teacher => teacher.Id == IdentityDataSeeder.StudentUserId);
        Assert.DoesNotContain(teachers, teacher => teacher.Name == "Demo Student");
        Assert.DoesNotContain(teachers, teacher => teacher.Id == IdentityDataSeeder.TeacherBUserId);
        Assert.DoesNotContain(teachers, teacher => teacher.Name == "Demo Teacher School B");
        Assert.DoesNotContain(teachers, teacher => teacher.Name == "Other Teacher");
    }

    [Fact]
    public async Task Parent_GetsTheirChild_WithIdAndNameOnly()
    {
        _factory.ParentTeachers.ChildUserIds =
        [
            IdentityDataSeeder.StudentUserId,
            IdentityDataSeeder.TeacherUserId,
            IdentityDataSeeder.StudentBUserId
        ];

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/parents/me/children-names",
            IdentityDataSeeder.ParentUserId,
            DefaultTenant.Id,
            PlatformRoles.Parent);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var children = document.RootElement.EnumerateArray().ToList();
        Assert.Contains(children, child =>
            child.GetProperty("id").GetString() == IdentityDataSeeder.StudentUserId
            && child.GetProperty("name").GetString() == "Demo Student");
        Assert.DoesNotContain(children, child => child.GetProperty("id").GetString() == IdentityDataSeeder.TeacherUserId);
        Assert.DoesNotContain(children, child => child.GetProperty("id").GetString() == IdentityDataSeeder.StudentBUserId);
        Assert.DoesNotContain(IdentityDataSeeder.StudentEmail, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("roles", body, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> ResolveAdminUserIdAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(IdentityDataSeeder.AdminEmail);
        Assert.NotNull(admin);
        return admin.Id;
    }
}
