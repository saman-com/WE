using Microsoft.EntityFrameworkCore;
using ReportingService.Application;
using ReportingService.Infrastructure.Data.Edw;

namespace ReportingService.Infrastructure.Analytics;

public sealed class LongitudinalAnalyticsQuery(EdwAnalyticsDbContext db) : ILongitudinalAnalyticsQuery
{
    private static readonly HashSet<string> ClosedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Closed",
        "Completed"
    };

    public async Task<StudentLongitudinalResponse> GetStudentLongitudinalAsync(
        Guid organisationId,
        string studentUserId,
        CancellationToken cancellationToken = default)
    {
        var evidenceFacts = await db.EvidenceFacts.AsNoTracking()
            .Where(f => f.OrganisationId == organisationId && f.StudentUserId == studentUserId)
            .Join(
                db.DimTimes.IgnoreQueryFilters().AsNoTracking(),
                fact => fact.TimeKey,
                dim => dim.DateKey,
                (fact, dim) => new { fact.MicroSkillCount, dim.Year, dim.Month })
            .ToListAsync(cancellationToken);

        var masteryTrend = BuildMasteryTrend(evidenceFacts.Select(e => (e.Year, e.Month, e.MicroSkillCount)));

        var interventionFacts = await db.InterventionFacts.AsNoTracking()
            .Where(f => f.OrganisationId == organisationId && f.StudentUserId == studentUserId)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        var gapHistory = BuildGapHistory(interventionFacts);
        var interventionOutcomes = interventionFacts
            .Select(f => new InterventionOutcomePoint(
                f.InterventionId,
                f.LearningGapId,
                f.Status,
                f.CreatedAt))
            .ToList();

        return new StudentLongitudinalResponse(
            organisationId,
            studentUserId,
            masteryTrend,
            gapHistory,
            interventionOutcomes);
    }

    public async Task<OrganisationLongitudinalResponse> GetOrganisationLongitudinalAsync(
        Guid organisationId,
        CancellationToken cancellationToken = default)
    {
        var evidenceFacts = await db.EvidenceFacts.AsNoTracking()
            .Where(f => f.OrganisationId == organisationId)
            .Join(
                db.DimTimes.IgnoreQueryFilters().AsNoTracking(),
                fact => fact.TimeKey,
                dim => dim.DateKey,
                (fact, dim) => new { fact.StudentUserId, fact.MicroSkillCount, dim.Year, dim.Month })
            .ToListAsync(cancellationToken);

        var interventionCounts = await db.InterventionFacts.AsNoTracking()
            .Where(f => f.OrganisationId == organisationId)
            .GroupBy(f => f.StudentUserId)
            .Select(g => new { StudentUserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var studentIds = evidenceFacts.Select(e => e.StudentUserId)
            .Concat(interventionCounts.Select(i => i.StudentUserId))
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var summaries = new List<StudentLongitudinalSummary>();
        foreach (var studentId in studentIds)
        {
            var studentEvidence = evidenceFacts.Where(e => e.StudentUserId == studentId).ToList();
            var cumulative = studentEvidence.Sum(e => e.MicroSkillCount);
            var interventionCount = interventionCounts
                .FirstOrDefault(i => i.StudentUserId == studentId)?.Count ?? 0;

            var studentInterventions = await db.InterventionFacts.AsNoTracking()
                .Where(f => f.OrganisationId == organisationId && f.StudentUserId == studentId)
                .ToListAsync(cancellationToken);

            summaries.Add(new StudentLongitudinalSummary(
                studentId,
                cumulative,
                interventionCount,
                BuildGapHistory(studentInterventions).Count));
        }

        var schoolMasteryTrend = BuildMasteryTrend(
            evidenceFacts.Select(e => (e.Year, e.Month, e.MicroSkillCount)));

        return new OrganisationLongitudinalResponse(
            organisationId,
            summaries,
            schoolMasteryTrend);
    }

    private static List<MasteryTrendPoint> BuildMasteryTrend(
        IEnumerable<(int Year, int Month, int MicroSkillCount)> evidence)
    {
        var grouped = evidence
            .GroupBy(e => e.Year * 100 + e.Month)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                PeriodKey = g.Key,
                MicroSkillsRecorded = g.Sum(x => x.MicroSkillCount)
            })
            .ToList();

        var cumulative = 0;
        return grouped
            .Select(g =>
            {
                cumulative += g.MicroSkillsRecorded;
                return new MasteryTrendPoint(g.PeriodKey, g.MicroSkillsRecorded, cumulative);
            })
            .ToList();
    }

    private static List<GapHistoryEvent> BuildGapHistory(IReadOnlyList<EdwInterventionFact> interventions)
    {
        var events = new List<GapHistoryEvent>();

        foreach (var group in interventions.GroupBy(i => i.LearningGapId))
        {
            var ordered = group.OrderBy(i => i.CreatedAt).ToList();
            var first = ordered[0];
            events.Add(new GapHistoryEvent(first.LearningGapId, "Opened", first.CreatedAt));

            var closed = ordered.LastOrDefault(i => ClosedStatuses.Contains(i.Status));
            if (closed is not null && ClosedStatuses.Contains(closed.Status))
            {
                events.Add(new GapHistoryEvent(closed.LearningGapId, "Closed", closed.CreatedAt));
            }
        }

        return events.OrderBy(e => e.OccurredAt).ToList();
    }
}
