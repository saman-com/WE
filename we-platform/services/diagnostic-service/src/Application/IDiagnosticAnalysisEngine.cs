using WePlatform.Events;

namespace DiagnosticService.Application;

public interface IDiagnosticAnalysisEngine
{
    IReadOnlyList<AnalyzedMicroSkillDiagnostic> Analyze(EvidenceCreated evidence);
}
