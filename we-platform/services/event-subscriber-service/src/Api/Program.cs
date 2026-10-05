using EventSubscriberService.Api.Consumers;
using MassTransit;
using RabbitMQ.Client;
using WePlatform.Events;
using WePlatform.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

const string PlatformEventsExchange = "we.platform.events";

builder.Services.AddMassTransit(bus =>
{
    bus.AddConsumer<EvidenceCreatedConsumer>();
    bus.AddConsumer<AssessmentApprovedConsumer>();

    bus.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var username = builder.Configuration["RabbitMQ:Username"] ?? "we";
        var password = builder.Configuration["RabbitMQ:Password"] ?? "we_dev";

        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        cfg.MessageTopology.SetEntityNameFormatter(new PlatformEventEntityNameFormatter());

        cfg.ReceiveEndpoint("platform-events-subscriber", endpoint =>
        {
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind(PlatformEventsExchange, binding =>
            {
                binding.ExchangeType = ExchangeType.Topic;
                binding.RoutingKey = EvidenceCreated.EventType;
            });
            endpoint.Bind(PlatformEventsExchange, binding =>
            {
                binding.ExchangeType = ExchangeType.Topic;
                binding.RoutingKey = AssessmentApproved.EventType;
            });
            endpoint.ConfigureConsumer<EvidenceCreatedConsumer>(context);
            endpoint.ConfigureConsumer<AssessmentApprovedConsumer>(context);
        });
    });
});

var app = builder.Build();

app.UseWePlatformSecurityHeaders();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program;

internal sealed class PlatformEventEntityNameFormatter : IEntityNameFormatter
{
    public string FormatEntityName<T>() => "we.platform.events";
}
