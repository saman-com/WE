using MasteryService.Application;
using MassTransit;
using Microsoft.Extensions.Logging;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace MasteryService.Infrastructure.Messaging.Consumers;

public sealed class EvidenceCreatedConsumer(
    IMasteryProcessor processor,
    ITenantContext tenantContext,
    ILogger<EvidenceCreatedConsumer> logger) : IConsumer<EvidenceCreated>
{
    public async Task Consume(ConsumeContext<EvidenceCreated> context)
    {
        var message = context.Message;
        tenantContext.SetTenant(message.OrganisationId);

        logger.LogInformation(
            "Processing {EventType} event {EventId} for evidence {EvidenceId}, student {StudentUserId}",
            EvidenceCreated.EventType,
            message.EventId,
            message.EvidenceId,
            message.StudentUserId);

        await processor.ProcessEvidenceCreatedAsync(message, context.CancellationToken);
    }
}
