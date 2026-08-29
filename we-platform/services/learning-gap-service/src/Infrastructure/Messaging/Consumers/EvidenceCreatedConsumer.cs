using LearningGapService.Application;
using MassTransit;
using Microsoft.Extensions.Logging;
using WePlatform.Events;

namespace LearningGapService.Infrastructure.Messaging.Consumers;

public sealed class EvidenceCreatedConsumer(
    IGapProcessor processor,
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
