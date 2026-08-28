using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrganisationService.Infrastructure.Data;

namespace OrganisationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganisationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrganisationDb");

        services.AddDbContext<OrganisationDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("OrganisationService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
