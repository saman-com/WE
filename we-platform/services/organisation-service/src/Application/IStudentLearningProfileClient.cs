namespace OrganisationService.Application;

public sealed record StudentProfileEnrollmentSync(
    Guid OrganisationId,
    Guid ClassId,
    string ClassName,
    string ClassCode);

public sealed record StudentProfileSummaryData(
    string StudentUserId,
    int EvidenceCount,
    DateTimeOffset? LatestActivityAt);

public sealed record StudentProfileTimelineEntryData(
    Guid Id,
    string Title,
    DateTimeOffset RecordedAt);

public sealed record StudentProfileData(
    string StudentUserId,
    IReadOnlyList<StudentProfileTimelineEntryData> EvidenceTimeline);

public interface IStudentLearningProfileClient
{
    Task SyncEnrollmentAsync(
        string studentUserId,
        StudentProfileEnrollmentSync enrollment,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<StudentProfileSummaryData?> GetProfileSummaryAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<StudentProfileData?> GetProfileAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
