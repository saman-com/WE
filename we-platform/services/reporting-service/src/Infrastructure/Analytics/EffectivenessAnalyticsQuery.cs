using Microsoft.EntityFrameworkCore;
using ReportingService.Application;
using ReportingService.Infrastructure.Data.Edw;

namespace ReportingService.Infrastructure.Analytics;

public sealed class EffectivenessAnalyticsQuery(EdwAnalyticsDbContext db) : IEffectivenessAnalyticsQuery
{
    private static readonly HashSet<string> SuccessfulStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Closed",
        "Completed"
    };

    public async Task<OrganisationEffectivenessResponse> GetOrganisationEffectivenessAsync(
        Guid organisationId,
        CancellationToken cancellationToken = default)
    {
        var curriculumFacts = await db.EvidenceFacts.AsNoTracking()
            .Where(f => f.OrganisationId == organisationId && f.UnitId != null)
            .ToListAsync(cancellationToken);

        var totalMastered = curriculumFacts.Sum(f => f.MasteredMicroSkillCount);
        var totalMicroSkills = curriculumFacts.Sum(f => f.TotalMicroSkillCount);
        var schoolAverageRate = totalMicroSkills > 0
            ? (double)totalMastered / totalMicroSkills
            : 0d;

        var curriculumEffectiveness = curriculumFacts
            .GroupBy(f => new { f.SubjectId, f.SubjectName, f.UnitId, f.UnitName })
            .Select(g =>
            {
                var mastered = g.Sum(f => f.MasteredMicroSkillCount);
                var total = g.Sum(f => f.TotalMicroSkillCount);
                var rate = total > 0 ? (double)mastered / total : 0d;

                return new CurriculumEffectivenessItem(
                    g.Key.SubjectId!.Value,
                    g.Key.SubjectName ?? string.Empty,
                    g.Key.UnitId!.Value,
                    g.Key.UnitName ?? string.Empty,
                    Math.Round(rate, 4),
                    total,
                    mastered,
                    rate < schoolAverageRate);
            })
            .OrderBy(c => c.MasteryRate)
            .ToList();

        var interventionFacts = await db.InterventionFacts.AsNoTracking()
            .Where(f => f.OrganisationId == organisationId && f.InterventionType != null)
            .ToListAsync(cancellationToken);

        var interventionEffectiveness = interventionFacts
            .GroupBy(f => f.InterventionType!)
            .Select(g =>
            {
                var total = g.Count();
                var successful = g.Count(f => SuccessfulStatuses.Contains(f.Status));
                var rate = total > 0 ? (double)successful / total : 0d;

                return new InterventionEffectivenessItem(
                    g.Key,
                    total,
                    successful,
                    Math.Round(rate, 4));
            })
            .OrderByDescending(i => i.SuccessRate)
            .ToList();

        return new OrganisationEffectivenessResponse(
            organisationId,
            curriculumEffectiveness,
            interventionEffectiveness);
    }
}
