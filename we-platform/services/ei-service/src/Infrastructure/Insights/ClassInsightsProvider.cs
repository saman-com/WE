using EiService.Application;
using EiService.Infrastructure.Organisation;

namespace EiService.Infrastructure.Insights;

public sealed class ClassInsightsProvider(
    IClassRosterClient rosterClient,
    IStudentEiDataClient studentEiDataClient,
    IClassInsightsAggregationEngine aggregationEngine) : IClassInsightsProvider
{
    public async Task<ClassEiInsightsResponse?> GetClassInsightsAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
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

        return aggregationEngine.Aggregate(new ClassInsightsInput(
            organisationId,
            classId,
            studentUserIds,
            students));
    }
}
