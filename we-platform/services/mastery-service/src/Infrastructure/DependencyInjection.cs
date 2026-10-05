using MasteryService.Application;
using MasteryService.Infrastructure.Data;
using MasteryService.Infrastructure.Messaging;
using MasteryService.Infrastructure.Organisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WePlatform.Tenancy;

namespace MasteryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMasteryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddWePlatformTenancy();
        var connectionString = configuration.GetConnectionString("MasteryDb");
        var thresholds = configuration.GetSection("Mastery").Get<MasteryThresholds>() ?? new MasteryThresholds();

        services.AddSingleton(thresholds);

        services.AddDbContext<MasteryDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("MasteryService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IMasteryCalculationEngine, MasteryCalculationEngine>();
        services.AddScoped<IMasteryProcessor, MasteryProcessor>();
        services.AddHttpClient<IOrganisationAccessChecker, HttpOrganisationAccessChecker>();
        services.AddMemoryCache();
        services.AddMasteryMessaging(configuration, environment);
        return services;
    }
}
