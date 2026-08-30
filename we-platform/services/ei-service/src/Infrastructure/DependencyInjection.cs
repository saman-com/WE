using EiService.Application;
using EiService.Infrastructure.Insights;
using EiService.Infrastructure.Organisation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EiService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEiInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClassInsightsAggregationEngine, ClassInsightsAggregationEngine>();
        services.AddHttpClient<IClassAccessChecker, HttpClassAccessChecker>();
        services.AddHttpClient<IClassRosterClient, HttpClassRosterClient>();
        services.AddHttpClient<IStudentEiDataClient, HttpStudentEiDataClient>();
        services.AddScoped<IClassInsightsProvider, ClassInsightsProvider>();
        return services;
    }
}
