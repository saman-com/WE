using EdwIngestService.Application;
using MassTransit;
using Microsoft.Extensions.Logging;
using WePlatform.Events;

namespace EdwIngestService.Infrastructure.Messaging.Consumers;

public sealed class AssessmentApprovedConsumer(
    IEdwIngestProcessor processor,
    ILogger<AssessmentApprovedConsumer> logger) : IConsumer<AssessmentApproved>
{
    public async Task Consume(ConsumeContext<AssessmentApproved> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Ingesting {EventType} event {EventId} for assessment {AssessmentId}",
            AssessmentApproved.EventType,
            message.EventId,
            message.AssessmentId);

        await processor.ProcessAssessmentApprovedAsync(message, context.CancellationToken);
    }
}
