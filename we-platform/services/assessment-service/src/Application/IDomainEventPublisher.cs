using WePlatform.Events;

namespace AssessmentService.Application;

public interface IDomainEventPublisher
{
    Task PublishAssessmentPublishedAsync(AssessmentPublished domainEvent, CancellationToken cancellationToken = default);
}
