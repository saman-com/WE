namespace StudentLearningService.Domain;

using WePlatform.Tenancy;

public sealed class ProfileClassEnrollment : ITenantEntity, IOrganisationTenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProfileId { get; set; }
    public StudentLearningProfile Profile { get; set; } = null!;
    public Guid OrganisationId { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ClassCode { get; set; } = string.Empty;
    public DateTimeOffset EnrolledAt { get; set; }
}
