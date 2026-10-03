namespace CurriculumService.Domain;

using WePlatform.Tenancy;

public sealed class LearningObjective : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UnitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Guid? SourceNodeId { get; set; }
    public bool IsOverridden { get; set; }

    public Unit Unit { get; set; } = null!;
    public ICollection<MicroSkill> MicroSkills { get; set; } = new List<MicroSkill>();
}
