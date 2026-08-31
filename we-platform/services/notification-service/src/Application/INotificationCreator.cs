namespace NotificationService.Application;

public interface INotificationCreator
{
    Task CreateAsync(
        string recipientUserId,
        string type,
        string title,
        string body,
        Guid sourceEventId,
        string sourceEventType,
        Guid? relatedEntityId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
