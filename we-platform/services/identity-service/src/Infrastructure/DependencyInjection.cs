using IdentityService.Application.Auth;
using IdentityService.Infrastructure.Auth;
using IdentityService.Infrastructure.Data;
using IdentityService.Infrastructure.Organisation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Tenancy;

namespace IdentityService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddWePlatformTenancy();
        var connectionString = configuration.GetConnectionString("IdentityDb");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var databaseName = configuration["InMemoryDatabaseName"] ?? "IdentityService";
                options.UseInMemoryDatabase(databaseName);
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services
            .AddIdentity<Domain.ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddScoped<JwtTokenService>();
        services.AddHttpClient<IParentClassTeacherSource, HttpParentClassTeacherSource>();

        return services;
    }
}
