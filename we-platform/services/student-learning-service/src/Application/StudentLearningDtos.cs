namespace StudentLearningService.Application;

public sealed record ClassEnrollmentSummary(
    Guid OrganisationId,
    Guid ClassId,
    string ClassName,
    string ClassCode,
    DateTimeOffset EnrolledAt);

public sealed record EvidenceTimelineEntry(
    Guid Id,
    string Title,
    DateTimeOffset RecordedAt);

public sealed record StudentProfileResponse(
    string StudentUserId,
    IReadOnlyList<ClassEnrollmentSummary> Enrollments,
    IReadOnlyList<EvidenceTimelineEntry> EvidenceTimeline);

public sealed record SyncProfileEnrollmentRequest(
    Guid OrganisationId,
    Guid ClassId,
    string ClassName,
    string ClassCode);

public sealed record RecordProfileEvidenceRequest(
    Guid EvidenceId,
    Guid AssessmentId,
    IReadOnlyList<Guid> MicroSkillIds,
    string Title,
    DateTimeOffset RecordedAt);
