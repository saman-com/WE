using AiGatewayService.Application;
using AiGatewayService.Infrastructure.Audit;
using AiGatewayService.Infrastructure.Context;
using AiGatewayService.Infrastructure.Data;
using AiGatewayService.Infrastructure.Prompts;
using AiGatewayService.Infrastructure.Providers;
using AiGatewayService.Infrastructure.Safety;
using AiGatewayService.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WePlatform.Tenancy;

namespace AiGatewayService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiGatewayInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddWePlatformTenancy();
        var connectionString = configuration.GetConnectionString("AiGatewayDb");

        services.AddDbContext<AiGatewayDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString) || environment.IsEnvironment("Testing"))
            {
                options.UseInMemoryDatabase("AiGatewayService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddSingleton<IPromptRegistry, FilePromptRegistry>();
        services.AddSingleton<IContextBuilder, StubContextBuilder>();
        services.AddSingleton<IResponseValidator, StubResponseValidator>();
        services.AddSingleton<ISafetyFilter, GovernanceSafetyFilter>();
        services.AddScoped<IAiAuditLogger, AiAuditLogger>();
        services.AddScoped<IAiAuditQueryService, AiAuditQueryService>();
        services.AddScoped<IAiCompletionService, AiCompletionService>();

        var provider = configuration["AiProvider:Provider"] ?? "Mock";
        if (environment.IsEnvironment("Testing") || string.Equals(provider, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAiProviderAdapter, MockAiProviderAdapter>();
        }
        else
        {
            services.AddSingleton<IAiProviderAdapter, MockAiProviderAdapter>();
        }

        return services;
    }
}
