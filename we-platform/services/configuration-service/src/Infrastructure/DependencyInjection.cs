using ConfigurationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WePlatform.Tenancy;

namespace ConfigurationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddWePlatformTenancy();
        var connectionString = configuration.GetConnectionString("ConfigurationDb");
        var testingDatabaseName = configuration["Testing:DatabaseName"]
            ?? $"ConfigurationService-{Guid.NewGuid():N}";

        services.AddDbContext<ConfigurationDbContext>(options =>
        {
            if (environment.IsEnvironment("Testing") || string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase(testingDatabaseName);
                return;
            }

            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
