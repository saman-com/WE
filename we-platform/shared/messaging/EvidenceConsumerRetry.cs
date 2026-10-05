using MassTransit;
using Microsoft.Extensions.Configuration;

namespace WePlatform.Messaging;

public static class EvidenceConsumerRetry
{
    /// <summary>
    /// Immediate retries for transient faults, then delayed redelivery (default 1/5/15 minutes).
    /// Override with RabbitMQ:DelayedRedeliveryIntervalsSeconds as a comma-separated list
    /// (e.g. "2,5,10") for integration tests. Requires rabbitmq_delayed_message_exchange.
    /// </summary>
    public static void Configure(IReceiveEndpointConfigurator endpoint, IConfiguration configuration)
    {
        var delays = ParseDelays(configuration["RabbitMQ:DelayedRedeliveryIntervalsSeconds"]);

        endpoint.UseDelayedRedelivery(r => r.Intervals(delays));
        endpoint.UseMessageRetry(retry =>
            retry.Intervals(
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5)));
    }

    internal static TimeSpan[] ParseDelays(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15)];
        }

        var delays = configured
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => TimeSpan.FromSeconds(int.Parse(part)))
            .ToArray();

        return delays.Length > 0
            ? delays
            : [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15)];
    }
}
