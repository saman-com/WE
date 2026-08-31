namespace NotificationService.Domain;

using WePlatform.Tenancy;

public sealed class Notification : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
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
