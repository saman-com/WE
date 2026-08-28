namespace OrganisationService.Domain;

public sealed class ClassEnrollment
{
    public Guid ClassId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;

    public SchoolClass Class { get; set; } = null!;
}
