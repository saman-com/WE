namespace NationalReportingService.Domain;

public sealed class RegionalEquityFact
{
    public Guid Id { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string DemographicDimension { get; set; } = string.Empty;
    public string DemographicCategory { get; set; } = string.Empty;
    public decimal AverageMasteryPercent { get; set; }
    public int SampleSize { get; set; }
    public DateOnly AsOfDate { get; set; }
}
