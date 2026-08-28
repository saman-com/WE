namespace OrganisationService.Domain;

public sealed class YearLevel
{
    public Guid Id { get; set; }
    public Guid OrganisationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Organisation Organisation { get; set; } = null!;
    public ICollection<SchoolClass> Classes { get; set; } = new List<SchoolClass>();
}
