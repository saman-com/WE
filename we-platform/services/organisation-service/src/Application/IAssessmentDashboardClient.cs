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
    Task<IReadOnlyList<AssessmentSummaryData>> ListClassAssessmentSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentAssessmentSummaryData>> ListStudentAssessmentSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
