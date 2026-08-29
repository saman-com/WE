namespace EvidenceService.Domain;

public sealed class EvidenceMicroSkillMark
{
    public Guid EvidenceId { get; set; }
    public Guid MicroSkillId { get; set; }
    public decimal Mark { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public EducationalEvidence Evidence { get; set; } = null!;
}
