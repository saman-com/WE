namespace OrganisationService.Domain;

using WePlatform.Tenancy;

public sealed class OrganisationLeader : ITenantEntity, IOrganisationTenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrganisationId { get; set; }
    public string LeaderUserId { get; set; } = string.Empty;
    public DateTimeOffset AssignedAt { get; set; }

    public Organisation Organisation { get; set; } = null!;
}
