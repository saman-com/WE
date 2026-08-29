namespace AssessmentService.Domain;

public sealed class Assessment
{
    public Guid Id { get; set; }
    public Guid OrganisationId { get; set; }
    public Guid ClassId { get; set; }
    public string CreatedByTeacherUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public string Status { get; set; } = AssessmentStatuses.Draft;
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<AssessmentLearningObjective> LearningObjectives { get; set; } = [];
    public ICollection<AssessmentMicroSkill> MicroSkills { get; set; } = [];
    public ICollection<AssessmentSubmission> Submissions { get; set; } = [];
}
