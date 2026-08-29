using WePlatform.Events;

namespace DiagnosticService.Application;

public interface IDiagnosticProcessor
{
    Task ProcessEvidenceCreatedAsync(EvidenceCreated evidence, CancellationToken cancellationToken = default);
}
