using FederationService.Application;
using FederationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FederationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFederationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FederationDb");

        services.AddDbContext<FederationDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("FederationService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddHttpClient<IRegionalConfigurationProvisioner, HttpRegionalConfigurationProvisioner>();
        return services;
    }
}
