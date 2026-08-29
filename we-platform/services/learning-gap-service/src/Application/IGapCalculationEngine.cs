namespace LearningGapService.Application;

public interface IGapCalculationEngine
{
    CalculatedLearningGap? CalculateFromDiagnostic(MicroSkillDiagnosticInput diagnostic);

    IReadOnlyList<CalculatedLearningGap> CalculateFromDiagnostics(
        IReadOnlyList<MicroSkillDiagnosticInput> diagnostics);
}
