using OrganisationService.Application;

namespace OrganisationService.Tests;

public sealed class FakeInterventionDashboardClient : IInterventionDashboardClient
{
    public Dictionary<string, IReadOnlyList<ParentInterventionSummaryData>> InterventionsByStudent { get; } = [];

    public Task<IReadOnlyList<ParentInterventionSummaryData>> ListActiveInterventionsAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (InterventionsByStudent.TryGetValue(studentUserId, out var interventions))
        {
            return Task.FromResult(interventions);
        }

        return Task.FromResult<IReadOnlyList<ParentInterventionSummaryData>>([]);
    }
}
