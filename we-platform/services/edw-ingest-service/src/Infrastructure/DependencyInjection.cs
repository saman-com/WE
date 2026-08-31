using EdwIngestService.Application;
using EdwIngestService.Infrastructure.Data;
using EdwIngestService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EdwIngestService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEdwIngestInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("EdwDb");

        services.AddDbContext<EdwDbContext>(options =>
        {
            if (environment.IsEnvironment("Testing") || string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("EdwIngestService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IEdwIngestProcessor, EdwIngestProcessor>();
        services.AddEdwIngestMessaging(configuration, environment);
        return services;
    }
}
