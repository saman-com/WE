using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiGatewayService.Infrastructure.Providers;

/// <summary>
/// Mock is the only AI provider until a real adapter is integrated (deferred).
/// </summary>
public static class MockAiProviderStartup
{
    public const string ProviderName = "Mock";
    public const string AllowMockInProductionKey = "AiProvider:AllowMockInProduction";

    public static bool IsMockProvider(string? configuredProvider) =>
        string.IsNullOrWhiteSpace(configuredProvider)
        || string.Equals(configuredProvider, ProviderName, StringComparison.OrdinalIgnoreCase);

    public static void EnsureMockAllowedInEnvironment(
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        if (configuration.GetValue(AllowMockInProductionKey, false))
        {
            return;
        }

        throw new InvalidOperationException(
            "Mock AI provider is not permitted in Production. " +
            $"Set {AllowMockInProductionKey}=true to override until a real provider is configured.");
    }
}

internal sealed class MockAiProviderStartupWarning(
    ILogger<MockAiProviderStartupWarning> logger,
    IConfiguration configuration) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var configured = configuration["AiProvider:Provider"] ?? MockAiProviderStartup.ProviderName;
        logger.LogWarning(
            "AI provider is Mock (AiProvider:Provider={ConfiguredProvider}) — canned sample text only. " +
            "Real provider integration is deferred. Do not treat output as production AI feedback.",
            configured);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
