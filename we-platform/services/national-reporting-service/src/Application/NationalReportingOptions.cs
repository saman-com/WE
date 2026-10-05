namespace NationalReportingService.Application;

public sealed class NationalReportingOptions
{
    public const string SectionName = "NationalReporting";

    /// <summary>
    /// Minimum group size for disclosable national/policy aggregate counts.
    /// Cells below this threshold are returned as null with suppressed=true.
    /// </summary>
    public int MinimumGroupSize { get; set; } = 5;

    public int RateLimitPermitLimit { get; set; } = 60;

    public int RateLimitWindowSeconds { get; set; } = 60;
}
