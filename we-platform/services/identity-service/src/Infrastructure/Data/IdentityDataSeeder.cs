using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WePlatform.Tenancy;

namespace IdentityService.Infrastructure.Data;

public static class IdentityDataSeeder
{
    public const string TeacherEmail = "teacher@school.local";
    public const string TeacherPassword = "Password123!";
    public const string AdminEmail = "admin@school.local";
    public const string AdminPassword = "Password123!";
    public const string StudentEmail = "student@school.local";
    public const string StudentPassword = "Password123!";
    public const string StudentUserId = "22222222-2222-2222-2222-222222222222";
    public const string ParentEmail = "parent@school.local";
    public const string ParentPassword = "Password123!";
    public const string ParentUserId = "33333333-3333-3333-3333-333333333333";
    public const string AuthorityEmail = "authority@ministry.local";
    public const string AuthorityPassword = "Password123!";
    public const string AuthorityUserId = "44444444-4444-4444-4444-444444444444";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityDataSeeder");

        foreach (var role in PlatformRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(userManager, TeacherEmail, TeacherPassword, "Demo Teacher", PlatformRoles.Teacher);
        await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "Demo Admin", PlatformRoles.SystemAdministrator);
        await EnsureUserAsync(
            userManager,
            StudentEmail,
            StudentPassword,
            "Demo Student",
            PlatformRoles.Student,
            StudentUserId);
        await EnsureUserAsync(
            userManager,
            ParentEmail,
            ParentPassword,
            "Demo Parent",
            PlatformRoles.Parent,
            ParentUserId);
        await EnsureUserAsync(
            userManager,
            AuthorityEmail,
            AuthorityPassword,
            "Demo Authority Officer",
            PlatformRoles.EducationAuthorityOfficer,
            AuthorityUserId);

        logger.LogInformation("Identity seed data applied.");
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string displayName,
        string role,
        string? userId = null)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return;
        }

        user = new ApplicationUser
        {
            Id = userId ?? Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
            TenantId = DefaultTenant.Id
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, role);
    }
}
