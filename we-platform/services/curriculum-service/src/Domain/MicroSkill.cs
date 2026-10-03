namespace CurriculumService.Domain;

using WePlatform.Tenancy;

public sealed class MicroSkill : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid LearningObjectiveId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Guid? SourceNodeId { get; set; }
    public bool IsOverridden { get; set; }

    public LearningObjective LearningObjective { get; set; } = null!;
}
