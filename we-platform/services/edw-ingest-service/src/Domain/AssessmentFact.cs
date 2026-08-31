namespace EdwIngestService.Domain;

public sealed class AssessmentFact
{
    public Guid EventId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid OrganisationId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid EvidenceId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public string ApprovedByTeacherUserId { get; set; } = string.Empty;
    public DateTimeOffset ApprovedAt { get; set; }
    public int TimeKey { get; set; }
    public int MicroSkillCount { get; set; }
    public DateTimeOffset IngestedAt { get; set; }
}
