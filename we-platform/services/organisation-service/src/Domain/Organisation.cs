namespace OrganisationService.Domain;

public sealed class Organisation
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<YearLevel> YearLevels { get; set; } = new List<YearLevel>();
    public ICollection<SchoolClass> Classes { get; set; } = new List<SchoolClass>();
    public ICollection<OrganisationLeader> Leaders { get; set; } = new List<OrganisationLeader>();
}
