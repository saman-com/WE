using IdentityService.Domain;
using IdentityService.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IdentityService.Tests;

public class DemoUserSeedingTests
{
    [Fact]
    public async Task Production_DoesNotSeedDemoUsers()
    {
        await using var factory = new ProductionIdentityWebApplicationFactory();
        _ = factory.Server; // force host start / Program startup seeding gate

        var environment = factory.Services.GetRequiredService<IHostEnvironment>();
        Assert.Equal(Environments.Production, environment.EnvironmentName);

        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.Null(await userManager.FindByEmailAsync(IdentityDataSeeder.TeacherEmail));
        Assert.Null(await userManager.FindByEmailAsync(IdentityDataSeeder.AdminEmail));
        Assert.Null(await userManager.FindByEmailAsync(IdentityDataSeeder.StudentEmail));
        Assert.Empty(userManager.Users.ToList());
    }

    private sealed class ProductionIdentityWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"IdentityService_Production_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Production);
            // Avoid the Production Postgres connection string so this asserts seeding, not live data.
            builder.UseSetting("ConnectionStrings:IdentityDb", string.Empty);
            builder.UseSetting("InMemoryDatabaseName", _databaseName);
            builder.UseSetting("Jwt:Key", "test-signing-key-at-least-32-chars-long");
            builder.UseSetting("Jwt:Issuer", "we-platform-identity-test");
            builder.UseSetting("Jwt:Audience", "we-platform-test");
        }
    }
}
