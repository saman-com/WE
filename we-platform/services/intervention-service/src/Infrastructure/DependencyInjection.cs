using InterventionService.Application;
using InterventionService.Infrastructure.Data;
using InterventionService.Infrastructure.Messaging;
using InterventionService.Infrastructure.Organisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WePlatform.Tenancy;

namespace InterventionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInterventionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.AddWePlatformTenancy();
        var connectionString = configuration.GetConnectionString("InterventionDb");

        services.AddDbContext<InterventionDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var databaseName = configuration["InMemoryDatabaseName"] ?? "InterventionService";
                options.UseInMemoryDatabase(databaseName);
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddHttpClient<IOrganisationAccessChecker, HttpOrganisationAccessChecker>();
        if (environment is not null && !environment.IsEnvironment("Testing"))
        {
            services.AddInterventionMessaging(configuration);
        }
        else
        {
            services.AddSingleton<IInterventionEventPublisher, NullInterventionEventPublisher>();
        }

        return services;
    }
}
