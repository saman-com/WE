namespace CurriculumService.Domain;

public sealed class Topic
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Unit Unit { get; set; } = null!;
}
