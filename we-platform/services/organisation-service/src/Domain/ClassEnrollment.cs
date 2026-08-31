namespace OrganisationService.Domain;

using WePlatform.Tenancy;

public sealed class ClassEnrollment : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ClassId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;

    public SchoolClass Class { get; set; } = null!;
}
