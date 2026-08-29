using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EvidenceService.Infrastructure.Messaging;

public static class MessagingDependencyInjection
{
    public const string PlatformEventsExchange = "we.platform.events";

    public static IServiceCollection AddEvidenceMessaging(
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

                cfg.Publish<WePlatform.Events.EvidenceCreated>(publish => publish.ExchangeType = RabbitMQ.Client.ExchangeType.Topic);
                cfg.Publish<WePlatform.Events.AssessmentApproved>(publish => publish.ExchangeType = RabbitMQ.Client.ExchangeType.Topic);

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<Application.IDomainEventPublisher, MassTransitDomainEventPublisher>();
        return services;
    }

    private sealed class PlatformEventEntityNameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => PlatformEventsExchange;
    }
}
