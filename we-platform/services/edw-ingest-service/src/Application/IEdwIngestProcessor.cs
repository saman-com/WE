using WePlatform.Events;

namespace EdwIngestService.Application;

public interface IEdwIngestProcessor
{
    Task ProcessEvidenceCreatedAsync(EvidenceCreated evidence, CancellationToken cancellationToken = default);

    Task ProcessAssessmentApprovedAsync(AssessmentApproved assessment, CancellationToken cancellationToken = default);

    Task ProcessInterventionCreatedAsync(InterventionCreated intervention, CancellationToken cancellationToken = default);

    Task ProcessEvidenceBatchAsync(
        IReadOnlyList<EvidenceCreated> events,
        CancellationToken cancellationToken = default);
}
