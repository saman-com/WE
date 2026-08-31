using AiGatewayService.Application;
using AiGatewayService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace AiGatewayService.Infrastructure.Audit;

public sealed class AiAuditQueryService(
    AiGatewayDbContext db,
    ITenantContext tenantContext) : IAiAuditQueryService
{
    public async Task<IReadOnlyList<AiAuditLogResponse>> SearchAsync(
        AiAuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var entries = db.AiAuditLogs.AsNoTracking().AsQueryable();

        if (tenantContext.HasTenant)
        {
            entries = entries.Where(entry => entry.TenantId == tenantContext.TenantId);
        }

        if (!string.IsNullOrWhiteSpace(query.PromptId))
        {
            entries = entries.Where(entry => entry.PromptId == query.PromptId);
        }

        if (!string.IsNullOrWhiteSpace(query.Outcome))
        {
            entries = entries.Where(entry => entry.Outcome == query.Outcome);
        }

        if (!string.IsNullOrWhiteSpace(query.CallerUserId))
        {
            entries = entries.Where(entry => entry.CallerUserId == query.CallerUserId);
        }

        if (query.From.HasValue)
        {
            entries = entries.Where(entry => entry.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            entries = entries.Where(entry => entry.CreatedAt <= query.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            entries = entries.Where(entry =>
                entry.PromptId.Contains(search)
                || entry.ContextScope.Contains(search)
                || (entry.BlockReason != null && entry.BlockReason.Contains(search)));
        }

        var limit = query.Limit <= 0 ? 100 : Math.Min(query.Limit, 500);

        return await entries
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(limit)
            .Select(entry => new AiAuditLogResponse(
                entry.Id,
                entry.CallerUserId,
                entry.PromptId,
                entry.PromptVersion,
                entry.ContextScope,
                entry.Outcome,
                entry.BlockReason,
                entry.ProviderName,
                entry.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
