namespace EdwIngestService.Domain;

public sealed class DimTime
{
    public int DateKey { get; set; }
    public DateOnly CalendarDate { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}
