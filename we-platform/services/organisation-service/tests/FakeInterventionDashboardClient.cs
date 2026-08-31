using OrganisationService.Application;

namespace OrganisationService.Tests;

public sealed class FakeInterventionDashboardClient : IInterventionDashboardClient
{
    public Dictionary<string, IReadOnlyList<ParentInterventionSummaryData>> InterventionsByStudent { get; } = [];

    public Dictionary<(Guid OrganisationId, string? Status), IReadOnlyList<InterventionDetailData>> OrganisationInterventions { get; } = [];

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

    public Task<IReadOnlyList<InterventionDetailData>> ListOrganisationInterventionsAsync(
        Guid organisationId,
        string? status,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (OrganisationInterventions.TryGetValue((organisationId, status), out var interventions))
        {
            return Task.FromResult(interventions);
        }

        if (OrganisationInterventions.TryGetValue((organisationId, null), out var allInterventions))
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return Task.FromResult(allInterventions);
            }

            return Task.FromResult<IReadOnlyList<InterventionDetailData>>(
                allInterventions
                    .Where(item => string.Equals(item.Status, status, StringComparison.OrdinalIgnoreCase))
                    .ToList());
        }

        return Task.FromResult<IReadOnlyList<InterventionDetailData>>([]);
    }
}
