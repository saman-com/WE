namespace CurriculumService.Domain;

public sealed class Unit
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Subject Subject { get; set; } = null!;
    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
