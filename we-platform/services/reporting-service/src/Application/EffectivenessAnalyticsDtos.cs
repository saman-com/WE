namespace ReportingService.Application;

public sealed record CurriculumEffectivenessItem(
    Guid SubjectId,
    string SubjectName,
    Guid UnitId,
    string UnitName,
    double MasteryRate,
    int TotalMicroSkills,
    int MasteredMicroSkills,
    bool IsUnderperforming);

public sealed record InterventionEffectivenessItem(
    string InterventionType,
    int TotalCount,
    int SuccessfulCount,
    double SuccessRate);

public sealed record OrganisationEffectivenessResponse(
    Guid OrganisationId,
    IReadOnlyList<CurriculumEffectivenessItem> CurriculumEffectiveness,
    IReadOnlyList<InterventionEffectivenessItem> InterventionEffectiveness);

public interface IEffectivenessAnalyticsQuery
{
    Task<OrganisationEffectivenessResponse> GetOrganisationEffectivenessAsync(
        Guid organisationId,
        CancellationToken cancellationToken = default);
}
