using InterventionService.Application;
using InterventionService.Infrastructure.Data;
using InterventionService.Infrastructure.Organisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InterventionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInterventionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InterventionDb");

        services.AddDbContext<InterventionDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("InterventionService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddHttpClient<IOrganisationAccessChecker, HttpOrganisationAccessChecker>();
        return services;
    }
}
