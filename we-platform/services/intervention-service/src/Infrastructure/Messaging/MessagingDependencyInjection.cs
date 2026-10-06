using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Events;

namespace InterventionService.Infrastructure.Messaging;

public static class MessagingDependencyInjection
{
    public const string PlatformEventsExchange = "we.platform.events";

    public static IServiceCollection AddInterventionMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(bus =>
        {
            bus.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMQ:Host"] ?? "localhost";
                var username = configuration["RabbitMQ:Username"] ?? "we";
                var password = configuration["RabbitMQ:Password"] ?? "we_dev";
                var port = ushort.TryParse(configuration["RabbitMQ:Port"], out var parsedPort)
                    ? parsedPort
                    : (ushort)5672;

                cfg.Host(host, port, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.MessageTopology.SetEntityNameFormatter(new PlatformEventEntityNameFormatter());
                cfg.Publish<InterventionCreated>(publish =>
                    publish.ExchangeType = RabbitMQ.Client.ExchangeType.Topic);
            });
        });

        services.AddScoped<Application.IInterventionEventPublisher, MassTransitInterventionEventPublisher>();
        services.AddHostedService<InterventionEventBackfill>();
        return services;
    }

    private sealed class PlatformEventEntityNameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => PlatformEventsExchange;
    }
}
