using CommunicationService.Application;
using CommunicationService.Infrastructure.Data;
using CommunicationService.Infrastructure.Organisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunicationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommunicationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CommunicationDb");

        services.AddDbContext<CommunicationDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("CommunicationService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddHttpClient<IOrganisationAccessChecker, HttpOrganisationAccessChecker>();
        return services;
    }
}
