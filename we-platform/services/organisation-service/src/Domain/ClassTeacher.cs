namespace OrganisationService.Domain;

public sealed class ClassTeacher
{
    public Guid ClassId { get; set; }
    public string TeacherUserId { get; set; } = string.Empty;

    public SchoolClass Class { get; set; } = null!;
}
