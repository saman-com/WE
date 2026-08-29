namespace OrganisationService.Application;

public sealed record AssessmentSummaryData(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset? DueAt,
    int SubmissionCount);

public interface IAssessmentDashboardClient
{
    Task<IReadOnlyList<AssessmentSummaryData>> ListClassAssessmentSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
