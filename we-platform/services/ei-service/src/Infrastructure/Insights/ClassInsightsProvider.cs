using System.Collections.Concurrent;
using EiService.Application;
using EiService.Infrastructure.Organisation;
using Microsoft.Extensions.Caching.Memory;

namespace EiService.Infrastructure.Insights;

public sealed class ClassInsightsProvider(
    IClassRosterClient rosterClient,
    IStudentEiDataClient studentEiDataClient,
    IClassInsightsAggregationEngine aggregationEngine,
    IMemoryCache cache) : IClassInsightsProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();

    public async Task<ClassEiInsightsResponse?> GetClassInsightsAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        // School-scoped only — never reuse across organisations.
        var cacheKey = $"ei:insights:{organisationId}:{classId}";
        if (cache.TryGetValue(cacheKey, out ClassEiInsightsResponse? cached) && cached is not null)
        {
            return cached;
        }

        var gate = Gates.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(cacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            var studentUserIds = await rosterClient.GetStudentUserIdsAsync(
                organisationId,
                classId,
                bearerToken,
                cancellationToken);
            if (studentUserIds is null)
            {
                return null;
            }

            var snapshotTasks = studentUserIds
                .Select(studentUserId => studentEiDataClient.GetStudentSnapshotAsync(
                    studentUserId,
                    bearerToken,
                    cancellationToken))
                .ToList();
            var snapshots = await Task.WhenAll(snapshotTasks);

            var students = snapshots
                .Where(snapshot => snapshot is not null)
                .Select(snapshot => snapshot!)
                .ToList();

            var insights = aggregationEngine.Aggregate(new ClassInsightsInput(
                organisationId,
                classId,
                studentUserIds,
                students));

            cache.Set(cacheKey, insights, CacheDuration);
            return insights;
        }
        finally
        {
            gate.Release();
        }
    }
}
