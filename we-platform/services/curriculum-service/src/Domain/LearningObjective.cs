namespace CurriculumService.Domain;

public sealed class LearningObjective
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Unit Unit { get; set; } = null!;
    public ICollection<MicroSkill> MicroSkills { get; set; } = new List<MicroSkill>();
}
