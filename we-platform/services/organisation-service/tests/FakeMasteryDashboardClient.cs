using OrganisationService.Application;

namespace OrganisationService.Tests;

public sealed class FakeMasteryDashboardClient : IMasteryDashboardClient
{
    public Dictionary<string, IReadOnlyList<ParentMasterySummaryData>> RecordsByStudent { get; } = [];

    public Task<IReadOnlyList<ParentMasterySummaryData>> ListStudentMasteryAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (RecordsByStudent.TryGetValue(studentUserId, out var records))
        {
            return Task.FromResult(records);
        }

        return Task.FromResult<IReadOnlyList<ParentMasterySummaryData>>([]);
    }
}
