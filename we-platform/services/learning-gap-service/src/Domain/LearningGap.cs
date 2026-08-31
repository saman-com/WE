namespace LearningGapService.Domain;

using WePlatform.Tenancy;

public sealed class LearningGap : ITenantEntity, IOrganisationTenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public Guid OrganisationId { get; set; }
    public Guid EvidenceId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid MicroSkillId { get; set; }
    public Guid? LearningObjectiveId { get; set; }
    public string ExpectedMastery { get; set; } = string.Empty;
    public string ActualMastery { get; set; } = string.Empty;
    public decimal Mark { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
