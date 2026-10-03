namespace NationalReportingService.Domain;

public sealed class RegionalInterventionImpactFact
{
    public Guid Id { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string InterventionType { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SuccessfulCount { get; set; }
    public decimal AverageGrowthPercent { get; set; }
    public DateOnly AsOfDate { get; set; }
}
