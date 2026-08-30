namespace NotificationService.Domain;

public sealed class Notification
{
    public Guid Id { get; set; }
    public string RecipientUserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid SourceEventId { get; set; }
    public string SourceEventType { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
