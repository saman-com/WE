namespace CommunicationService.Domain;

public sealed class ParentTeacherMessage
{
    public Guid Id { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public string ParentUserId { get; set; } = string.Empty;
    public string TeacherUserId { get; set; } = string.Empty;
    public string SenderUserId { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
