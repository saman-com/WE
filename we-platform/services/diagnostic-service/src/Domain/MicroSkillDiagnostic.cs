namespace DiagnosticService.Domain;

public sealed class MicroSkillDiagnostic
{
    public Guid Id { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public Guid OrganisationId { get; set; }
    public Guid EvidenceId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid MicroSkillId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Mark { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
