namespace NationalReportingService.Domain;

public sealed class RegionalMasteryFact
{
    public Guid Id { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public decimal AverageMasteryPercent { get; set; }
    public decimal MasteredSharePercent { get; set; }
    public int SampleSize { get; set; }
    public DateOnly AsOfDate { get; set; }
}
