using LearningGapService.Application;
using LearningGapService.Infrastructure.Data;
using LearningGapService.Infrastructure.Messaging;
using LearningGapService.Infrastructure.Organisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LearningGapService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGapInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("GapDb");

        services.AddDbContext<GapDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("LearningGapService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IGapCalculationEngine, GapCalculationEngine>();
        services.AddScoped<IGapProcessor, GapProcessor>();
        services.AddHttpClient<IOrganisationAccessChecker, HttpOrganisationAccessChecker>();
        services.AddGapMessaging(configuration, environment);
        return services;
    }
}
