namespace FederationService.Domain;

public sealed class FederationPolicy
{
    public Guid Id { get; set; }
    public Guid FederationId { get; set; }
    public string PolicyKey { get; set; } = string.Empty;
    public string PolicyValue { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
