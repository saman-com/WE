namespace InterventionService.Application;

public sealed record CreateInterventionRequest(
    Guid OrganisationId,
    string StudentUserId,
    Guid LearningGapId,
    string PlannedActions,
    string? Notes,
    DateTimeOffset? PlannedStartAt,
    DateTimeOffset? PlannedEndAt,
    DateTimeOffset? ReviewAt);

public sealed record PatchInterventionRequest(
    string? Status,
    string? Notes,
    string? Outcome,
    string? PlannedActions,
    DateTimeOffset? PlannedStartAt,
    DateTimeOffset? PlannedEndAt,
    DateTimeOffset? ReviewAt);

public sealed record InterventionResponse(
    Guid Id,
    Guid OrganisationId,
    string StudentUserId,
    Guid LearningGapId,
    string AssignedTeacherUserId,
    string PlannedActions,
    string Notes,
    string? Outcome,
    string Status,
    DateTimeOffset? PlannedStartAt,
    DateTimeOffset? PlannedEndAt,
    DateTimeOffset? ReviewAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record StudentInterventionsResponse(
    string StudentUserId,
    IReadOnlyList<InterventionResponse> Interventions);

public sealed record ParentInterventionSummaryResponse(
    Guid Id,
    string Summary,
    string Status,
    DateTimeOffset? PlannedStartAt,
    DateTimeOffset? PlannedEndAt);

public sealed record ParentInterventionsResponse(
    string StudentUserId,
    IReadOnlyList<ParentInterventionSummaryResponse> Interventions);

public sealed record OrganisationInterventionsResponse(
    Guid OrganisationId,
    IReadOnlyList<InterventionResponse> Interventions);
