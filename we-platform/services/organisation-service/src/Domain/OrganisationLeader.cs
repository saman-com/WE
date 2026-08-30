namespace OrganisationService.Domain;

public sealed class OrganisationLeader
{
    public Guid OrganisationId { get; set; }
    public string LeaderUserId { get; set; } = string.Empty;
    public DateTimeOffset AssignedAt { get; set; }

    public Organisation Organisation { get; set; } = null!;
}
