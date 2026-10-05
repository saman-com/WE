namespace OrganisationService.Application;

public sealed record AssessmentSummaryData(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset? DueAt,
    int SubmissionCount);

public sealed record StudentAssessmentSummaryData(
    Guid Id,
    string Title,
    DateTimeOffset? DueAt,
    IReadOnlyList<Guid> LearningObjectiveIds,
    bool HasSubmitted,
    DateTimeOffset? SubmittedAt);

public interface IAssessmentDashboardClient
{
    /// <summary>
    /// Recent class assessments only (explicit small page — not a full inventory).
    /// </summary>
    Task<IReadOnlyList<AssessmentSummaryData>> GetRecentClassAssessmentSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentAssessmentSummaryData>> ListStudentAssessmentSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentAssessmentSummaryData>> ListStudentAssessmentSummariesForStudentAsync(
        Guid organisationId,
        Guid classId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
