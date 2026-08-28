namespace CurriculumService.Domain;

public sealed class Subject
{
    public Guid Id { get; set; }
    public Guid CurriculumId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Curriculum Curriculum { get; set; } = null!;
    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
