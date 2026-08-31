namespace AssessmentService.Domain;

using WePlatform.Tenancy;

public sealed class AssessmentMicroSkill : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid MicroSkillId { get; set; }
    public Assessment Assessment { get; set; } = null!;
}
