namespace InterventionService.Domain;

public sealed class Intervention
{
    public Guid Id { get; set; }
    public Guid OrganisationId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public Guid LearningGapId { get; set; }
    public string AssignedTeacherUserId { get; set; } = string.Empty;
    public string PlannedActions { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string? Outcome { get; set; }
    public string Status { get; set; } = InterventionStatuses.Planned;
    public DateTimeOffset? PlannedStartAt { get; set; }
    public DateTimeOffset? PlannedEndAt { get; set; }
    public DateTimeOffset? ReviewAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
