namespace OrganisationService.Domain;

using WePlatform.Tenancy;

public sealed class ParentStudentLink : ITenantEntity
{
    public Guid TenantId { get; set; }
    public required string ParentUserId { get; set; }
    public required string StudentUserId { get; set; }
    public DateTimeOffset LinkedAt { get; set; }
}
