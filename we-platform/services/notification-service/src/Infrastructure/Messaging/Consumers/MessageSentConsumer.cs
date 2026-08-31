using MassTransit;
using NotificationService.Application;
using NotificationService.Domain;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace NotificationService.Infrastructure.Messaging.Consumers;

public sealed class MessageSentConsumer(INotificationCreator notificationCreator) : IConsumer<MessageSent>
{
    public async Task Consume(ConsumeContext<MessageSent> context)
    {
        var message = context.Message;
        await notificationCreator.CreateAsync(
            message.RecipientUserId,
            NotificationTypes.NewMessage,
            "New message received",
            message.BodyPreview,
            message.EventId,
            MessageSent.EventType,
            message.MessageId,
            DefaultTenant.Id,
            context.CancellationToken);
    }
}
