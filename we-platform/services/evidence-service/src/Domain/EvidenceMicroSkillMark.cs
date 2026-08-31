namespace EvidenceService.Domain;

using WePlatform.Tenancy;

public sealed class EvidenceMicroSkillMark : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EvidenceId { get; set; }
    public Guid MicroSkillId { get; set; }
    public decimal Mark { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public EducationalEvidence Evidence { get; set; } = null!;
}
