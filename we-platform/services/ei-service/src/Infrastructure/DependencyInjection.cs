using EiService.Application;
using EiService.Infrastructure.Ai;
using EiService.Infrastructure.Data;
using EiService.Infrastructure.Insights;
using EiService.Infrastructure.Organisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WePlatform.Tenancy;

namespace EiService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEiInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddWePlatformTenancy();
        var connectionString = configuration.GetConnectionString("EiDb");

        services.AddDbContext<EiDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString) || environment.IsEnvironment("Testing"))
            {
                options.UseInMemoryDatabase("EiService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddSingleton<IClassInsightsAggregationEngine, ClassInsightsAggregationEngine>();
        services.AddHttpClient<IClassAccessChecker, HttpClassAccessChecker>();
        services.AddMemoryCache();
        services.AddHttpClient<IClassRosterClient, HttpClassRosterClient>();
        services.AddHttpClient<IStudentEiDataClient, HttpStudentEiDataClient>();
        services.AddScoped<IClassInsightsProvider, ClassInsightsProvider>();
        services.AddSingleton<ISummaryContextBuilder, SummaryContextBuilder>();
        services.AddSingleton<ServiceJwtIssuer>();
        services.AddHttpClient<IAiGatewayClient, HttpAiGatewayClient>();
        services.AddScoped<AiSummaryDraftService>();
        return services;
    }
}
