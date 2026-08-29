using MassTransit;
using WePlatform.Events;

namespace EventSubscriberService.Api.Consumers;

public sealed class AssessmentApprovedConsumer(ILogger<AssessmentApprovedConsumer> logger) : IConsumer<AssessmentApproved>
{
    public Task Consume(ConsumeContext<AssessmentApproved> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Received {EventType} event {EventId} for assessment {AssessmentId}, student {StudentUserId}, evidence {EvidenceId} with {ResultCount} micro-skill results",
            AssessmentApproved.EventType,
            message.EventId,
            message.AssessmentId,
            message.StudentUserId,
            message.EvidenceId,
            message.MicroSkillResults.Count);
        return Task.CompletedTask;
    }
}
