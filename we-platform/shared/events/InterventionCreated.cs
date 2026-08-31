namespace WePlatform.Events;

/// <summary>
/// Published when a teacher creates an intervention for a student learning gap.
/// Immutable once published (append-only).
/// </summary>
public sealed record InterventionCreated(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid OrganisationId,
    int Version,
    Guid InterventionId,
    string StudentUserId,
    Guid LearningGapId,
    string AssignedTeacherUserId,
    string Status,
    DateTimeOffset CreatedAt)
{
    public const int CurrentVersion = 1;
    public const string EventType = nameof(InterventionCreated);
}
