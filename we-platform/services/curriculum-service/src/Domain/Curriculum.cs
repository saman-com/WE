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
    public string? RegionCode { get; set; }
    public string Scope { get; set; } = CurriculumScopes.School;
    public Guid? ParentCurriculumId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Curriculum? ParentCurriculum { get; set; }
    public ICollection<Curriculum> ChildCurricula { get; set; } = new List<Curriculum>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
