namespace EdwIngestService.Domain;

public sealed class InterventionFact
{
    public Guid EventId { get; set; }
    public Guid InterventionId { get; set; }
    public Guid OrganisationId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public Guid LearningGapId { get; set; }
    public string AssignedTeacherUserId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int TimeKey { get; set; }
    public DateTimeOffset IngestedAt { get; set; }
}
