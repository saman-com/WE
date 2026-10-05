using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NationalReportingService.Application;
using NationalReportingService.Infrastructure.Data;
using NationalReportingService.Infrastructure.Reporting;

namespace NationalReportingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNationalReportingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<NationalReportingOptions>(
            configuration.GetSection(NationalReportingOptions.SectionName));

        var connectionString = configuration.GetConnectionString("NationalReportingDb");

        var testingDatabaseName = configuration["Testing:DatabaseName"]
            ?? $"NationalReportingService-{Guid.NewGuid():N}";

        services.AddDbContext<NationalReportingDbContext>(options =>
        {
            if (environment.IsEnvironment("Testing") || string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase(testingDatabaseName);
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<INationalReportQuery, NationalReportQuery>();
        services.AddScoped<IPolicyDashboardQuery, PolicyDashboardQuery>();
        return services;
    }
}
