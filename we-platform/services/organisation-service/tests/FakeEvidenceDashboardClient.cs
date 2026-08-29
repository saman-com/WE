using OrganisationService.Application;

namespace OrganisationService.Tests;

public sealed class FakeEvidenceDashboardClient : IEvidenceDashboardClient
{
    public IReadOnlyList<EvidenceSummaryData> Summaries { get; set; } = [];

    public Task<IReadOnlyList<EvidenceSummaryData>> ListClassEvidenceSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Summaries);

    public IReadOnlyList<StudentEvidenceFeedbackData> StudentFeedback { get; set; } = [];

    public Task<IReadOnlyList<StudentEvidenceFeedbackData>> ListStudentFeedbackAsync(
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(StudentFeedback);
}
