using DiagnosticService.Application;
using DiagnosticService.Infrastructure.Data;
using DiagnosticService.Infrastructure.Messaging;
using DiagnosticService.Infrastructure.Organisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiagnosticService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDiagnosticInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DiagnosticDb");

        services.AddDbContext<DiagnosticDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("DiagnosticService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IDiagnosticAnalysisEngine, DiagnosticAnalysisEngine>();
        services.AddScoped<IDiagnosticProcessor, DiagnosticProcessor>();
        services.AddHttpClient<IOrganisationAccessChecker, HttpOrganisationAccessChecker>();
        services.AddDiagnosticMessaging(configuration, environment);
        return services;
    }
}
