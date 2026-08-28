namespace CurriculumService.Domain;

public sealed class MicroSkill
{
    public Guid Id { get; set; }
    public Guid LearningObjectiveId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public LearningObjective LearningObjective { get; set; } = null!;
}
