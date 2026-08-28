namespace StudentLearningService.Domain;

public sealed class ProfileClassEnrollment
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public StudentLearningProfile Profile { get; set; } = null!;
    public Guid OrganisationId { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ClassCode { get; set; } = string.Empty;
    public DateTimeOffset EnrolledAt { get; set; }
}
