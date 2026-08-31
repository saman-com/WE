namespace AssessmentService.Domain;

using WePlatform.Tenancy;

public sealed class AssessmentLearningObjective : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid LearningObjectiveId { get; set; }
    public Assessment Assessment { get; set; } = null!;
}
