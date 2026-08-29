namespace WePlatform.Events;

/// <summary>
/// Published when a teacher approves evidence. Immutable once published (append-only).
/// </summary>
public sealed record EvidenceCreated(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid OrganisationId,
    int Version,
    Guid EvidenceId,
    Guid AssessmentId,
    Guid SubmissionId,
    string StudentUserId,
    Guid ClassId,
    string ApprovedByTeacherUserId,
    DateTimeOffset ApprovedAt,
    IReadOnlyList<MicroSkillResult> MicroSkillMarks)
{
    public const int CurrentVersion = 1;
    public const string EventType = nameof(EvidenceCreated);
}
