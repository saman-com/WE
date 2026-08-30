using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationService.Application;
using NotificationService.Infrastructure.Data;
using NotificationService.Infrastructure.Email;
using NotificationService.Infrastructure.Messaging.Consumers;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using WePlatform.Events;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public const string PlatformEventsExchange = "we.platform.events";

    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("NotificationDb");

        services.AddDbContext<NotificationDbContext>(options =>
        {
            if (environment.IsEnvironment("Testing") || string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("NotificationService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<INotificationCreator, NotificationCreator>();
        services.AddSingleton<IEmailNotifier, MockEmailNotifier>();

        if (!environment.IsEnvironment("Testing"))
        {
            services.AddNotificationMessaging(configuration);
        }

        return services;
    }

    private static IServiceCollection AddNotificationMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<AssessmentPublishedConsumer>();
            bus.AddConsumer<AssessmentApprovedConsumer>();
            bus.AddConsumer<MessageSentConsumer>();

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

                cfg.ReceiveEndpoint("notification-service-events", endpoint =>
                {
                    endpoint.ConfigureConsumeTopology = false;
                    endpoint.Bind(PlatformEventsExchange, binding =>
                    {
                        binding.ExchangeType = ExchangeType.Topic;
                        binding.RoutingKey = AssessmentPublished.EventType;
                    });
                    endpoint.Bind(PlatformEventsExchange, binding =>
                    {
                        binding.ExchangeType = ExchangeType.Topic;
                        binding.RoutingKey = AssessmentApproved.EventType;
                    });
                    endpoint.Bind(PlatformEventsExchange, binding =>
                    {
                        binding.ExchangeType = ExchangeType.Topic;
                        binding.RoutingKey = MessageSent.EventType;
                    });
                    endpoint.ConfigureConsumer<AssessmentPublishedConsumer>(context);
                    endpoint.ConfigureConsumer<AssessmentApprovedConsumer>(context);
                    endpoint.ConfigureConsumer<MessageSentConsumer>(context);
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
