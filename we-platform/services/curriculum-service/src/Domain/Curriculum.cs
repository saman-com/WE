namespace CurriculumService.Domain;

using WePlatform.Tenancy;

public sealed class Curriculum : ITenantEntity, IOrganisationTenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganisationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = CurriculumStatuses.Draft;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
