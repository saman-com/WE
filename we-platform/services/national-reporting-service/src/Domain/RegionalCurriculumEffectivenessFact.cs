namespace NationalReportingService.Domain;

public sealed class RegionalCurriculumEffectivenessFact
{
    public Guid Id { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string CurriculumCode { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public decimal MasteryRatePercent { get; set; }
    public decimal CoveragePercent { get; set; }
    public int SchoolsReporting { get; set; }
    public DateOnly AsOfDate { get; set; }
}
