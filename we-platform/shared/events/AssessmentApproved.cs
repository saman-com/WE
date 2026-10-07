namespace WePlatform.Events;

/// <summary>
/// Published when a teacher approves an assessment submission via evidence approval.
/// Immutable once published (append-only).
/// </summary>
public sealed record AssessmentApproved(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid OrganisationId,
    int Version,
    Guid AssessmentId,
    string StudentUserId,
    Guid SubmissionId,
    Guid EvidenceId,
    string ApprovedByTeacherUserId,
    DateTimeOffset ApprovedAt,
    IReadOnlyList<MicroSkillResult> MicroSkillResults,
    string AssessmentTitle = "")
{
    public const int CurrentVersion = 1;
    public const string EventType = nameof(AssessmentApproved);
}
