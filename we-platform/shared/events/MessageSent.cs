namespace WePlatform.Events;

/// <summary>
/// Published when a parent or teacher sends a message in a student context.
/// </summary>
public sealed record MessageSent(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    int Version,
    Guid MessageId,
    string StudentUserId,
    string SenderUserId,
    string RecipientUserId,
    string BodyPreview)
{
    public const int CurrentVersion = 1;
    public const string EventType = nameof(MessageSent);
}
