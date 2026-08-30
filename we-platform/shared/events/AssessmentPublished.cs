namespace WePlatform.Events;

/// <summary>
/// Published when a teacher publishes an assessment to a class.
/// </summary>
public sealed record AssessmentPublished(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid OrganisationId,
    int Version,
    Guid AssessmentId,
    Guid ClassId,
    string Title,
    string PublishedByTeacherUserId,
    IReadOnlyList<string> RecipientUserIds)
{
    public const int CurrentVersion = 1;
    public const string EventType = nameof(AssessmentPublished);
}
