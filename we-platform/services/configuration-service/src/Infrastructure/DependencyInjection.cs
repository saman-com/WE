using ConfigurationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Tenancy;

namespace ConfigurationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddWePlatformTenancy();
        var connectionString = configuration.GetConnectionString("ConfigurationDb");

        services.AddDbContext<ConfigurationDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("ConfigurationService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
