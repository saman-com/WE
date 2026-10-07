using MassTransit;
using NotificationService.Application;
using NotificationService.Domain;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace NotificationService.Infrastructure.Messaging.Consumers;

public sealed class AssessmentApprovedConsumer(INotificationCreator notificationCreator) : IConsumer<AssessmentApproved>
{
    public async Task Consume(ConsumeContext<AssessmentApproved> context)
    {
        var message = context.Message;
        var assessmentTitle = message.AssessmentTitle.Trim();
        var title = string.IsNullOrEmpty(assessmentTitle)
            ? "Teacher feedback available"
            : $"Teacher feedback available: {assessmentTitle}";
        var body = string.IsNullOrEmpty(assessmentTitle)
            ? "Your teacher has approved assessment feedback. Review your results when ready."
            : $"Your teacher has approved feedback for \"{assessmentTitle}\". Review your results when ready.";
        await notificationCreator.CreateAsync(
            message.StudentUserId,
            NotificationTypes.FeedbackAvailable,
            title,
            body,
            message.EventId,
            AssessmentApproved.EventType,
            message.AssessmentId,
            message.OrganisationId,
            context.CancellationToken);
    }
}
