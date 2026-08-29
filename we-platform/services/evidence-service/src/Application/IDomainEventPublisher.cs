using WePlatform.Events;

namespace EvidenceService.Application;

public interface IDomainEventPublisher
{
    Task PublishEvidenceCreatedAsync(EvidenceCreated domainEvent, CancellationToken cancellationToken = default);

    Task PublishAssessmentApprovedAsync(AssessmentApproved domainEvent, CancellationToken cancellationToken = default);
}
