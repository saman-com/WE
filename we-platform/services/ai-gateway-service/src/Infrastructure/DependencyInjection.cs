using AiGatewayService.Application;
using AiGatewayService.Infrastructure.Context;
using AiGatewayService.Infrastructure.Prompts;
using AiGatewayService.Infrastructure.Providers;
using AiGatewayService.Infrastructure.Safety;
using AiGatewayService.Infrastructure.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AiGatewayService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiGatewayInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton<IPromptRegistry, FilePromptRegistry>();
        services.AddSingleton<IContextBuilder, StubContextBuilder>();
        services.AddSingleton<IResponseValidator, StubResponseValidator>();
        services.AddSingleton<ISafetyFilter, StubSafetyFilter>();
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
