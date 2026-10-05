using WePlatform.Events;

namespace StudentLearningService.Application;

public interface IProfileEvidenceProcessor
{
    Task ProcessEvidenceCreatedAsync(EvidenceCreated evidence, CancellationToken cancellationToken = default);
}
