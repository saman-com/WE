namespace NationalReportingService.Domain;

public sealed class RegionalCoverageFact
{
    public Guid Id { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string CurriculumCode { get; set; } = string.Empty;
    public decimal CoveredObjectivePercent { get; set; }
    public int SchoolsReporting { get; set; }
    public DateOnly AsOfDate { get; set; }
}
