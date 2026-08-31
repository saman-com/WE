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

    Task<IReadOnlyList<InterventionDetailData>> ListOrganisationInterventionsAsync(
        Guid organisationId,
        string? status,
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public sealed record InterventionDetailData(
    Guid Id,
    Guid OrganisationId,
    string StudentUserId,
    Guid LearningGapId,
    string AssignedTeacherUserId,
    string PlannedActions,
    string? Outcome,
    string Status,
    DateTimeOffset? PlannedStartAt,
    DateTimeOffset? PlannedEndAt,
    DateTimeOffset? ReviewAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
