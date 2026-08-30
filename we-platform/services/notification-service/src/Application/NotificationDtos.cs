namespace NotificationService.Application;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Body,
    Guid? RelatedEntityId,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public sealed record NotificationListResponse(IReadOnlyList<NotificationResponse> Notifications);

public sealed record EmailNotificationRequest(
    string RecipientUserId,
    string Subject,
    string Body);
