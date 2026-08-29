using DiagnosticService.Domain;
using WePlatform.Events;

namespace DiagnosticService.Application;

public sealed class DiagnosticAnalysisEngine : IDiagnosticAnalysisEngine
{
    public IReadOnlyList<AnalyzedMicroSkillDiagnostic> Analyze(EvidenceCreated evidence)
    {
        return evidence.MicroSkillMarks
            .Select(mark => AnalyzeMark(evidence, mark))
            .ToList();
    }

    public static string ClassifyMark(decimal mark) =>
        mark >= 4 ? DiagnosticStatus.Mastered :
        mark >= 2 ? DiagnosticStatus.Developing :
        DiagnosticStatus.Struggling;

    private static AnalyzedMicroSkillDiagnostic AnalyzeMark(EvidenceCreated evidence, MicroSkillResult result)
    {
        var status = ClassifyMark(result.Mark);
        var reason =
            $"Evidence {evidence.EvidenceId}: micro-skill {result.MicroSkillId} marked {result.Mark}/5. " +
            $"Teacher feedback: \"{result.Feedback}\". Classification: {status}.";

        return new AnalyzedMicroSkillDiagnostic(
            evidence.EvidenceId,
            evidence.AssessmentId,
            result.MicroSkillId,
            status,
            result.Mark,
            reason);
    }
}
