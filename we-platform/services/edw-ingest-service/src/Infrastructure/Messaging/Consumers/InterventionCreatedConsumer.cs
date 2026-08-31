using EdwIngestService.Application;
using MassTransit;
using Microsoft.Extensions.Logging;
using WePlatform.Events;

namespace EdwIngestService.Infrastructure.Messaging.Consumers;

public sealed class InterventionCreatedConsumer(
    IEdwIngestProcessor processor,
    ILogger<InterventionCreatedConsumer> logger) : IConsumer<InterventionCreated>
{
    public async Task Consume(ConsumeContext<InterventionCreated> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Ingesting {EventType} event {EventId} for intervention {InterventionId}",
            InterventionCreated.EventType,
            message.EventId,
            message.InterventionId);

        await processor.ProcessInterventionCreatedAsync(message, context.CancellationToken);
    }
}
