using DiagnosticService.Application;
using MassTransit;
using Microsoft.Extensions.Logging;
using WePlatform.Events;

namespace DiagnosticService.Infrastructure.Messaging.Consumers;

public sealed class EvidenceCreatedConsumer(
    IDiagnosticProcessor processor,
    ILogger<EvidenceCreatedConsumer> logger) : IConsumer<EvidenceCreated>
{
    public async Task Consume(ConsumeContext<EvidenceCreated> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Processing {EventType} event {EventId} for evidence {EvidenceId}, student {StudentUserId}",
            EvidenceCreated.EventType,
            message.EventId,
            message.EvidenceId,
            message.StudentUserId);

        await processor.ProcessEvidenceCreatedAsync(message, context.CancellationToken);
    }
}
