using MassTransit;
using WePlatform.Events;

namespace EventSubscriberService.Api.Consumers;

public sealed class EvidenceCreatedConsumer(ILogger<EvidenceCreatedConsumer> logger) : IConsumer<EvidenceCreated>
{
    public Task Consume(ConsumeContext<EvidenceCreated> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Received {EventType} event {EventId} for evidence {EvidenceId}, assessment {AssessmentId}, student {StudentUserId}",
            EvidenceCreated.EventType,
            message.EventId,
            message.EvidenceId,
            message.AssessmentId,
            message.StudentUserId);
        return Task.CompletedTask;
    }
}
