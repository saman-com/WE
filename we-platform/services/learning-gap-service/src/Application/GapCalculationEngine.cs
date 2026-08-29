using LearningGapService.Domain;

namespace LearningGapService.Application;

public sealed class GapCalculationEngine : IGapCalculationEngine
{
    public CalculatedLearningGap? CalculateFromDiagnostic(MicroSkillDiagnosticInput diagnostic)
    {
        if (diagnostic.Status == DiagnosticStatus.Mastered)
        {
            return null;
        }

        var severity = DetermineSeverity(diagnostic.Status, diagnostic.Mark);
        var urgency = DetermineUrgency(diagnostic.Status, diagnostic.Mark);
        var explanation =
            $"Expected mastery: {ExpectedMastery.Mastered}. " +
            $"Demonstrated: {diagnostic.Status} (mark {diagnostic.Mark}/5). " +
            $"Micro-skill {diagnostic.MicroSkillId} has a {severity.ToLowerInvariant()}-severity gap. " +
            $"Based on diagnostic: {diagnostic.DiagnosticReason}";

        return new CalculatedLearningGap(
            diagnostic.EvidenceId,
            diagnostic.AssessmentId,
            diagnostic.MicroSkillId,
            ExpectedMastery.Mastered,
            diagnostic.Status,
            diagnostic.Mark,
            severity,
            urgency,
            explanation);
    }

    public IReadOnlyList<CalculatedLearningGap> CalculateFromDiagnostics(
        IReadOnlyList<MicroSkillDiagnosticInput> diagnostics) =>
        diagnostics
            .Select(CalculateFromDiagnostic)
            .Where(gap => gap is not null)
            .Select(gap => gap!)
            .ToList();

    public static string DetermineSeverity(string status, decimal mark) =>
        status == DiagnosticStatus.Struggling || mark <= 1 ? GapSeverity.High :
        mark <= 2 ? GapSeverity.Medium :
        GapSeverity.Low;

    public static string DetermineUrgency(string status, decimal mark) =>
        status == DiagnosticStatus.Struggling ? GapUrgency.High :
        mark <= 2 ? GapUrgency.Medium :
        GapUrgency.Low;
}
