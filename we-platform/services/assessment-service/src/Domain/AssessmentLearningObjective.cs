namespace AssessmentService.Domain;

public sealed class AssessmentLearningObjective
{
    public Guid AssessmentId { get; set; }
    public Guid LearningObjectiveId { get; set; }
    public Assessment Assessment { get; set; } = null!;
}
