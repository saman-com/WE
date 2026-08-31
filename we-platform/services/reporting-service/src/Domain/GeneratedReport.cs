namespace ReportingService.Domain;

using WePlatform.Tenancy;

public sealed class GeneratedReport : ITenantEntity, IOrganisationTenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public Guid OrganisationId { get; set; }
    public Guid? ClassId { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public string ContentJson { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
}
