namespace OrganisationService.Application;

public sealed record ParentMasterySummaryData(Guid MicroSkillId, string MasteryLevel);

public sealed record ParentInterventionSummaryData(
    Guid Id,
    string Summary,
    string Status,
    DateTimeOffset? PlannedStartAt,
    DateTimeOffset? PlannedEndAt);

public interface IMasteryDashboardClient
{
    Task<IReadOnlyList<ParentMasterySummaryData>> ListStudentMasteryAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public interface IInterventionDashboardClient
{
    Task<IReadOnlyList<ParentInterventionSummaryData>> ListActiveInterventionsAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
