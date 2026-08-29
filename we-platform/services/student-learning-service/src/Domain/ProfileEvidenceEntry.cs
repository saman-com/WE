namespace StudentLearningService.Domain;

public sealed class ProfileEvidenceEntry
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public StudentLearningProfile Profile { get; set; } = null!;
    public Guid AssessmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; }
    public List<ProfileEvidenceMicroSkill> MicroSkills { get; set; } = [];
}
