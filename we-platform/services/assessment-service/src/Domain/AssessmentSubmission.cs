namespace AssessmentService.Domain;

public sealed class AssessmentSubmission
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public string Responses { get; set; } = string.Empty;
    public string Status { get; set; } = SubmissionStatuses.Submitted;
    public bool IsLate { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Assessment Assessment { get; set; } = null!;
}
