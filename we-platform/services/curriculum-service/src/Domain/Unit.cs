namespace CurriculumService.Domain;

using WePlatform.Tenancy;

public sealed class Unit : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SubjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Guid? SourceNodeId { get; set; }
    public bool IsOverridden { get; set; }

    public Subject Subject { get; set; } = null!;
    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<LearningObjective> LearningObjectives { get; set; } = new List<LearningObjective>();
}
