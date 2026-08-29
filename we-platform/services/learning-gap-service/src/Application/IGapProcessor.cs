using WePlatform.Events;

namespace LearningGapService.Application;

public interface IGapProcessor
{
    Task ProcessEvidenceCreatedAsync(EvidenceCreated evidence, CancellationToken cancellationToken = default);
}
