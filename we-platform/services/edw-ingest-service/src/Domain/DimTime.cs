namespace EdwIngestService.Domain;

using WePlatform.Tenancy;

public sealed class DimTime : ITenantEntity
{
    public Guid TenantId { get; set; }
    public int DateKey { get; set; }
    public DateOnly CalendarDate { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}
