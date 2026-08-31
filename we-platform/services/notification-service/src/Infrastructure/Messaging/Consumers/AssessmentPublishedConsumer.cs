using MassTransit;
using NotificationService.Application;
using NotificationService.Domain;
using WePlatform.Events;

namespace NotificationService.Infrastructure.Messaging.Consumers;

public sealed class AssessmentPublishedConsumer(INotificationCreator notificationCreator) : IConsumer<AssessmentPublished>
{
    public async Task Consume(ConsumeContext<AssessmentPublished> context)
    {
        var message = context.Message;
        foreach (var recipientUserId in message.RecipientUserIds)
        {
            await notificationCreator.CreateAsync(
                recipientUserId,
                NotificationTypes.AssessmentPublished,
                $"Assessment available: {message.Title}",
                $"A new assessment \"{message.Title}\" is now available for your class.",
                message.EventId,
                AssessmentPublished.EventType,
                message.AssessmentId,
                message.OrganisationId,
                context.CancellationToken);
        }
    }
}
