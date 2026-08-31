using System.Text.RegularExpressions;
using AiGatewayService.Application;
using AiGatewayService.Domain;
using AiGatewayService.Infrastructure.Data;
using WePlatform.Tenancy;

namespace AiGatewayService.Infrastructure.Audit;

public sealed partial class AiAuditLogger(
    AiGatewayDbContext db,
    ITenantContext tenantContext) : IAiAuditLogger
{
    public async Task LogAsync(
        string callerUserId,
        string promptId,
        string promptVersion,
        string contextScope,
        string promptVariablesJson,
        string aiResponse,
        string outcome,
        string? blockReason,
        string providerName,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        db.AiAuditLogs.Add(new AiAuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId ?? DefaultTenant.Id,
            CallerUserId = callerUserId,
            PromptId = promptId,
            PromptVersion = promptVersion,
            ContextScope = contextScope,
            PromptVariablesJson = RedactPii(promptVariablesJson),
            AiResponse = RedactPii(aiResponse),
            Outcome = outcome,
            BlockReason = blockReason,
            ProviderName = providerName,
            CreatedAt = createdAt
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    internal static string RedactPii(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var redacted = EmailPattern().Replace(content, "[REDACTED_EMAIL]");
        redacted = PhonePattern().Replace(redacted, "[REDACTED_PHONE]");
        redacted = SsnPattern().Replace(redacted, "[REDACTED_ID]");
        return redacted;
    }

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b(?:\+?\d{1,3}[-.\s]?)?(?:\(\d{3}\)|\d{3})[-.\s]?\d{3}[-.\s]?\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex SsnPattern();
}
