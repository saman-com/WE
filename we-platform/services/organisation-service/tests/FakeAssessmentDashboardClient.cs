using OrganisationService.Application;

namespace OrganisationService.Tests;

public sealed class FakeAssessmentDashboardClient : IAssessmentDashboardClient
{
    public List<(Guid OrganisationId, Guid ClassId)> RequestedClasses { get; } = [];
    public IReadOnlyList<AssessmentSummaryData> Summaries { get; set; } = [];
    public Dictionary<Guid, IReadOnlyList<AssessmentSummaryData>> SummariesByClass { get; set; } = new();

    public Task<IReadOnlyList<AssessmentSummaryData>> GetRecentClassAssessmentSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        RequestedClasses.Add((organisationId, classId));
        if (SummariesByClass.TryGetValue(classId, out var classSummaries))
        {
            return Task.FromResult(classSummaries);
        }

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

    public Task<IReadOnlyList<StudentAssessmentSummaryData>> ListStudentAssessmentSummariesForStudentAsync(
        Guid organisationId,
        Guid classId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        RequestedStudentClasses.Add((organisationId, classId));
        return Task.FromResult(StudentSummaries);
    }
}
