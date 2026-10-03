namespace NationalReportingService.Domain;

public sealed class MinistryApiKey
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
