using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Email;

/// <summary>
/// Mock email is the only notifier until a real provider is integrated (deferred).
/// </summary>
public static class MockEmailStartup
{
    public const string AllowMockInProductionKey = "Email:AllowMockInProduction";

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
            "Mock email notifier is not permitted in Production. " +
            $"Set {AllowMockInProductionKey}=true to override until a real email provider is configured.");
    }
}

internal sealed class MockEmailStartupWarning(
    ILogger<MockEmailStartupWarning> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Email notifier is Mock — messages are logged only, not delivered. " +
            "Real email provider integration is deferred.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
