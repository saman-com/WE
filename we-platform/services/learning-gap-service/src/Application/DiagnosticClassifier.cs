using LearningGapService.Domain;
using WePlatform.Events;

namespace LearningGapService.Application;

public static class DiagnosticClassifier
{
    public static string ClassifyMark(decimal mark) =>
        mark >= 4 ? DiagnosticStatus.Mastered :
        mark >= 2 ? DiagnosticStatus.Developing :
        DiagnosticStatus.Struggling;

    public static MicroSkillDiagnosticInput ToDiagnosticInput(
        EvidenceCreated evidence,
        MicroSkillResult result)
    {
        var status = ClassifyMark(result.Mark);
        var reason =
            $"Marked {result.Mark}/5. " +
            $"Teacher feedback: \"{result.Feedback}\". Classification: {status}.";

        return new MicroSkillDiagnosticInput(
            evidence.EvidenceId,
            evidence.AssessmentId,
            result.MicroSkillId,
            status,
            result.Mark,
            reason);
    }
}
