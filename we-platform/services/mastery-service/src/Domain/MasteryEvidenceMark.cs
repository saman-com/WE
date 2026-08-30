namespace MasteryService.Domain;

public sealed class MasteryEvidenceMark
{
    public Guid Id { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public Guid OrganisationId { get; set; }
    public Guid MicroSkillId { get; set; }
    public Guid EvidenceId { get; set; }
    public Guid AssessmentId { get; set; }
    public decimal Mark { get; set; }
    public decimal Weight { get; set; } = 1m;
    public DateTimeOffset RecordedAt { get; set; }
}
