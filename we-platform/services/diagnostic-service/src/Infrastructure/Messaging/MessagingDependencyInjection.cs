using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DiagnosticService.Infrastructure.Messaging.Consumers;
using RabbitMQ.Client;
using WePlatform.Events;

namespace DiagnosticService.Infrastructure.Messaging;

public static class MessagingDependencyInjection
{
    public const string PlatformEventsExchange = "we.platform.events";

    public static IServiceCollection AddDiagnosticMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return services;
        }

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<EvidenceCreatedConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMQ:Host"] ?? "localhost";
                var username = configuration["RabbitMQ:Username"] ?? "we";
                var password = configuration["RabbitMQ:Password"] ?? "we_dev";

                cfg.Host(host, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.MessageTopology.SetEntityNameFormatter(new PlatformEventEntityNameFormatter());

                cfg.ReceiveEndpoint("diagnostic-service-evidence-created", endpoint =>
                {
                    endpoint.ConfigureConsumeTopology = false;
                    endpoint.Bind(PlatformEventsExchange, binding =>
                    {
                        binding.ExchangeType = ExchangeType.Topic;
                        binding.RoutingKey = EvidenceCreated.EventType;
                    });
                    endpoint.ConfigureConsumer<EvidenceCreatedConsumer>(context);
                });
            });
        });

        return services;
    }

    private sealed class PlatformEventEntityNameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => PlatformEventsExchange;
    }
}
