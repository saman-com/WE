namespace NationalReportingService.Domain;

public sealed class RegionalEnrollmentFact
{
    public Guid Id { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string SchoolCode { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int TeacherCount { get; set; }
    public DateOnly AsOfDate { get; set; }
}
