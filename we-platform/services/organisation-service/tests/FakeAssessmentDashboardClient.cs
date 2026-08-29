using OrganisationService.Application;

namespace OrganisationService.Tests;

public sealed class FakeAssessmentDashboardClient : IAssessmentDashboardClient
{
    public List<(Guid OrganisationId, Guid ClassId)> RequestedClasses { get; } = [];
    public IReadOnlyList<AssessmentSummaryData> Summaries { get; set; } = [];

    public Task<IReadOnlyList<AssessmentSummaryData>> ListClassAssessmentSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        RequestedClasses.Add((organisationId, classId));
        return Task.FromResult(Summaries);
    }

    public List<(Guid OrganisationId, Guid ClassId)> RequestedStudentClasses { get; } = [];
    public IReadOnlyList<StudentAssessmentSummaryData> StudentSummaries { get; set; } = [];

    public Task<IReadOnlyList<StudentAssessmentSummaryData>> ListStudentAssessmentSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        RequestedStudentClasses.Add((organisationId, classId));
        return Task.FromResult(StudentSummaries);
    }
}
