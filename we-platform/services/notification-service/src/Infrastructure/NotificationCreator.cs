using NotificationService.Application;
using NotificationService.Domain;
using NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Infrastructure;

public sealed class NotificationCreator(
    NotificationDbContext db,
    IEmailNotifier emailNotifier) : INotificationCreator
{
    public async Task CreateAsync(
        string recipientUserId,
        string type,
        string title,
        string body,
        Guid sourceEventId,
        string sourceEventType,
        Guid? relatedEntityId,
        CancellationToken cancellationToken = default)
    {
        if (!NotificationTypes.IsValid(type))
        {
            throw new ArgumentException($"Unsupported notification type: {type}", nameof(type));
        }

        var exists = await db.Notifications.AnyAsync(
            n => n.SourceEventId == sourceEventId && n.RecipientUserId == recipientUserId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        var notification = new Notification
        {
            Id = Guid.CreateVersion7(),
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Body = body,
            SourceEventId = sourceEventId,
            SourceEventType = sourceEventType,
            RelatedEntityId = relatedEntityId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        await emailNotifier.SendAsync(
            new EmailNotificationRequest(recipientUserId, title, body),
            cancellationToken);
    }
}
