namespace NationalReportingService.Application;

public record RegionTrendMetric(
    string RegionCode,
    int SchoolCount,
    int StudentCount,
    decimal AverageMasteryPercent);

public record PolicyTrendsResponse(
    DateOnly AsOfDate,
    int TotalSchools,
    int TotalStudents,
    decimal NationalAverageMasteryPercent,
    IReadOnlyList<RegionTrendMetric> Regions);

public record EquityMasteryDistribution(
    string RegionCode,
    string DemographicDimension,
    string DemographicCategory,
    decimal AverageMasteryPercent,
    int SampleSize);

public record EquityAnalysisResponse(
    DateOnly AsOfDate,
    IReadOnlyList<EquityMasteryDistribution> Distributions);

public record RegionCurriculumEffectiveness(
    string RegionCode,
    string CurriculumCode,
    string SubjectCode,
    decimal MasteryRatePercent,
    decimal CoveragePercent,
    int SchoolsReporting);

public record CurriculumEffectivenessComparisonResponse(
    DateOnly AsOfDate,
    IReadOnlyList<RegionCurriculumEffectiveness> Regions);

public record RegionInterventionImpact(
    string RegionCode,
    string InterventionType,
    int TotalCount,
    int SuccessfulCount,
    decimal SuccessRatePercent,
    decimal AverageGrowthPercent);

public record InterventionImpactResponse(
    DateOnly AsOfDate,
    IReadOnlyList<RegionInterventionImpact> Regions);

public interface IPolicyDashboardQuery
{
    Task<PolicyTrendsResponse> GetTrendsAsync(CancellationToken cancellationToken = default);

    Task<EquityAnalysisResponse> GetEquityAnalysisAsync(CancellationToken cancellationToken = default);

    Task<CurriculumEffectivenessComparisonResponse> GetCurriculumEffectivenessAsync(
        CancellationToken cancellationToken = default);

    Task<InterventionImpactResponse> GetInterventionImpactAsync(
        CancellationToken cancellationToken = default);
}
