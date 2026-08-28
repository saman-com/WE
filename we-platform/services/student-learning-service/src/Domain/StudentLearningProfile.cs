namespace StudentLearningService.Domain;

public sealed class StudentLearningProfile
{
    public Guid Id { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<ProfileClassEnrollment> Enrollments { get; set; } = [];
}
