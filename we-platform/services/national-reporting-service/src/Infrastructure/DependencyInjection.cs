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
        var connectionString = configuration.GetConnectionString("NationalReportingDb");

        services.AddDbContext<NationalReportingDbContext>(options =>
        {
            if (environment.IsEnvironment("Testing") || string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("NationalReportingService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<INationalReportQuery, NationalReportQuery>();
        return services;
    }
}
