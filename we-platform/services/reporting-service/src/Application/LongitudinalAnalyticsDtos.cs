namespace ReportingService.Application;

public sealed record MasteryTrendPoint(
    int PeriodKey,
    int MicroSkillsRecorded,
    int CumulativeMicroSkills);

public sealed record GapHistoryEvent(
    Guid LearningGapId,
    string EventType,
    DateTimeOffset OccurredAt);

public sealed record InterventionOutcomePoint(
    Guid InterventionId,
    Guid LearningGapId,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record StudentLongitudinalResponse(
    Guid OrganisationId,
    string StudentUserId,
    IReadOnlyList<MasteryTrendPoint> MasteryTrend,
    IReadOnlyList<GapHistoryEvent> GapHistory,
    IReadOnlyList<InterventionOutcomePoint> InterventionOutcomes);

public sealed record StudentLongitudinalSummary(
    string StudentUserId,
    int CumulativeMicroSkills,
    int InterventionCount,
    int GapEventCount);

public sealed record OrganisationLongitudinalResponse(
    Guid OrganisationId,
    IReadOnlyList<StudentLongitudinalSummary> StudentSummaries,
    IReadOnlyList<MasteryTrendPoint> SchoolMasteryTrend);

public interface ILongitudinalAnalyticsQuery
{
    Task<StudentLongitudinalResponse> GetStudentLongitudinalAsync(
        Guid organisationId,
        string studentUserId,
        CancellationToken cancellationToken = default);

    Task<OrganisationLongitudinalResponse> GetOrganisationLongitudinalAsync(
        Guid organisationId,
        CancellationToken cancellationToken = default);
}
