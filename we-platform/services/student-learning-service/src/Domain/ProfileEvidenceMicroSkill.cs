namespace StudentLearningService.Domain;

public sealed class ProfileEvidenceMicroSkill
{
    public Guid EvidenceEntryId { get; set; }
    public Guid MicroSkillId { get; set; }
    public ProfileEvidenceEntry EvidenceEntry { get; set; } = null!;
}
