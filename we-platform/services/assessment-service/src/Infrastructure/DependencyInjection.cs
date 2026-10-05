using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AssessmentService.Application;
using AssessmentService.Infrastructure.Ai;
using AssessmentService.Infrastructure.Data;
using AssessmentService.Infrastructure.Messaging;
using AssessmentService.Infrastructure.Organisation;
using WePlatform.Tenancy;

namespace AssessmentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAssessmentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddWePlatformTenancy();
        var connectionString = configuration.GetConnectionString("AssessmentDb");

        services.AddDbContext<AssessmentDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("AssessmentService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddHttpClient<IClassAccessChecker, HttpClassAccessChecker>();
        services.AddMemoryCache();
        services.AddHttpClient<IParentAccessChecker, HttpParentAccessChecker>();
        services.AddSingleton<ServiceJwtIssuer>();
        services.AddHttpClient<IAiGatewayClient, HttpAiGatewayClient>();
        services.AddScoped<AiFeedbackDraftService>();
        services.AddAssessmentMessaging(configuration, environment);

        return services;
    }
}
