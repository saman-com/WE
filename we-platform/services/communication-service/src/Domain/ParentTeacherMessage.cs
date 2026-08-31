namespace CommunicationService.Domain;

using WePlatform.Tenancy;

public sealed class ParentTeacherMessage : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public string ParentUserId { get; set; } = string.Empty;
    public string TeacherUserId { get; set; } = string.Empty;
    public string SenderUserId { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
