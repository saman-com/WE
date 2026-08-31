using EdwIngestService.Application;
using MassTransit;
using Microsoft.Extensions.Logging;
using WePlatform.Events;

namespace EdwIngestService.Infrastructure.Messaging.Consumers;

public sealed class EvidenceCreatedConsumer(
    IEdwIngestProcessor processor,
    ILogger<EvidenceCreatedConsumer> logger) : IConsumer<EvidenceCreated>
{
    public async Task Consume(ConsumeContext<EvidenceCreated> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Ingesting {EventType} event {EventId} for evidence {EvidenceId}",
            EvidenceCreated.EventType,
            message.EventId,
            message.EvidenceId);

        await processor.ProcessEvidenceCreatedAsync(message, context.CancellationToken);
    }
}
