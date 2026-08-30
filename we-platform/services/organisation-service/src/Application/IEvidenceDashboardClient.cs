namespace OrganisationService.Application;

public sealed record EvidenceSummaryData(Guid AssessmentId, int ReviewedCount);

public sealed record StudentEvidenceFeedbackData(
    Guid Id,
    Guid AssessmentId,
    string Title,
    DateTimeOffset ApprovedAt,
    IReadOnlyList<StudentEvidenceFeedbackMarkData> MicroSkillMarks);

public sealed record StudentEvidenceFeedbackMarkData(
    Guid MicroSkillId,
    decimal Mark,
    string Feedback);

public interface IEvidenceDashboardClient
{
    Task<IReadOnlyList<EvidenceSummaryData>> ListClassEvidenceSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentEvidenceFeedbackData>> ListStudentFeedbackAsync(
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentEvidenceFeedbackData>> ListStudentFeedbackForStudentAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
