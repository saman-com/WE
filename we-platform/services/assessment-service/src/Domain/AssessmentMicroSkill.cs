namespace AssessmentService.Domain;

public sealed class AssessmentMicroSkill
{
    public Guid AssessmentId { get; set; }
    public Guid MicroSkillId { get; set; }
    public Assessment Assessment { get; set; } = null!;
}
