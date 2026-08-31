using CommunicationService.Application;
using CommunicationService.Infrastructure.Data;
using CommunicationService.Infrastructure.Messaging;
using CommunicationService.Infrastructure.Organisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WePlatform.Tenancy;

namespace CommunicationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommunicationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddWePlatformTenancy();
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
        services.AddCommunicationMessaging(configuration, environment);
        return services;
    }
}
