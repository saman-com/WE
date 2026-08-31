namespace ReportingService.Domain;

public sealed class GeneratedReport
{
    public Guid Id { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public Guid OrganisationId { get; set; }
    public Guid? ClassId { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public string ContentJson { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
}
