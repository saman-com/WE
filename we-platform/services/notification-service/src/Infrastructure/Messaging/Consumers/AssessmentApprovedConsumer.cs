using MassTransit;
using NotificationService.Application;
using NotificationService.Domain;
using WePlatform.Events;

namespace NotificationService.Infrastructure.Messaging.Consumers;

public sealed class AssessmentApprovedConsumer(INotificationCreator notificationCreator) : IConsumer<AssessmentApproved>
{
    public async Task Consume(ConsumeContext<AssessmentApproved> context)
    {
        var message = context.Message;
        await notificationCreator.CreateAsync(
            message.StudentUserId,
            NotificationTypes.FeedbackAvailable,
            "Teacher feedback available",
            "Your teacher has approved assessment feedback. Review your results when ready.",
            message.EventId,
            AssessmentApproved.EventType,
            message.AssessmentId,
            context.CancellationToken);
    }
}
