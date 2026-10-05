using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NationalReportingService.Application;
using NationalReportingService.Infrastructure.Data;

namespace NationalReportingService.Infrastructure.Reporting;

public sealed class PolicyDashboardQuery(
    NationalReportingDbContext db,
    IOptions<NationalReportingOptions> options) : IPolicyDashboardQuery
{
    private int MinimumGroupSize => options.Value.MinimumGroupSize;

    public async Task<PolicyTrendsResponse> GetTrendsAsync(
        CancellationToken cancellationToken = default)
    {
        var enrollmentFacts = await db.RegionalEnrollmentFacts
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var masteryFacts = await db.RegionalMasteryFacts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var asOfCandidates = enrollmentFacts.Select(f => f.AsOfDate)
            .Concat(masteryFacts.Select(f => f.AsOfDate))
            .ToList();
        var asOf = asOfCandidates.Count == 0
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : asOfCandidates.Max();

        var enrollmentByRegion = enrollmentFacts
            .GroupBy(f => f.RegionCode)
            .ToDictionary(
                g => g.Key,
                g => (
                    SchoolCount: g.Select(f => f.SchoolCode).Distinct().Count(),
                    StudentCount: g.Sum(f => f.StudentCount)));

        var masteryByRegion = masteryFacts
            .GroupBy(f => f.RegionCode)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var sampleSize = g.Sum(f => f.SampleSize);
                    var sampleCell = CountCell.FromCount(sampleSize, MinimumGroupSize);
                    if (sampleCell.Suppressed || sampleSize == 0)
                    {
                        return (Sample: sampleCell, Average: (decimal?)null);
                    }

                    return (
                        Sample: sampleCell,
                        Average: (decimal?)decimal.Round(
                            g.Sum(f => f.AverageMasteryPercent * f.SampleSize) / sampleSize,
                            2));
                });

        var regionCodes = enrollmentByRegion.Keys
            .Union(masteryByRegion.Keys)
            .OrderBy(code => code)
            .ToList();

        var regions = regionCodes
            .Select(code =>
            {
                enrollmentByRegion.TryGetValue(code, out var enrollment);
                masteryByRegion.TryGetValue(code, out var mastery);
                var studentCount = CountCell.FromCount(enrollment.StudentCount, MinimumGroupSize);
                return new RegionTrendMetric(
                    code,
                    enrollment.SchoolCount,
                    studentCount,
                    mastery.Average);
            })
            .ToList();

        var nationalSampleCells = masteryByRegion.Values.Select(v => v.Sample).ToList();
        var nationalSampleSuppressed = nationalSampleCells.Count > 0
            && CountCell.SumOrSuppress(nationalSampleCells).Suppressed;
        decimal? nationalMastery = null;
        if (!nationalSampleSuppressed)
        {
            var nationalSample = masteryFacts.Sum(f => f.SampleSize);
            nationalMastery = nationalSample == 0
                ? 0m
                : decimal.Round(
                    masteryFacts.Sum(f => f.AverageMasteryPercent * f.SampleSize) / nationalSample,
                    2);
        }

        return new PolicyTrendsResponse(
            asOf,
            regions.Sum(r => r.SchoolCount),
            CountCell.SumOrSuppress(regions.Select(r => r.StudentCount)),
            nationalMastery,
            regions);
    }

    public async Task<EquityAnalysisResponse> GetEquityAnalysisAsync(
        CancellationToken cancellationToken = default)
    {
        var facts = await db.RegionalEquityFacts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var asOf = facts.Count == 0
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : facts.Max(f => f.AsOfDate);

        var distributions = facts
            .OrderBy(f => f.RegionCode)
            .ThenBy(f => f.DemographicDimension)
            .ThenBy(f => f.DemographicCategory)
            .Select(f =>
            {
                var sample = CountCell.FromCount(f.SampleSize, MinimumGroupSize);
                return new EquityMasteryDistribution(
                    f.RegionCode,
                    f.DemographicDimension,
                    f.DemographicCategory,
                    sample.Suppressed ? null : f.AverageMasteryPercent,
                    sample);
            })
            .ToList();

        return new EquityAnalysisResponse(asOf, distributions);
    }

    public async Task<CurriculumEffectivenessComparisonResponse> GetCurriculumEffectivenessAsync(
        CancellationToken cancellationToken = default)
    {
        var facts = await db.RegionalCurriculumEffectivenessFacts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var asOf = facts.Count == 0
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : facts.Max(f => f.AsOfDate);

        var regions = facts
            .OrderBy(f => f.RegionCode)
            .ThenBy(f => f.CurriculumCode)
            .ThenBy(f => f.SubjectCode)
            .Select(f =>
            {
                var schools = CountCell.FromCount(f.SchoolsReporting, MinimumGroupSize);
                return new RegionCurriculumEffectiveness(
                    f.RegionCode,
                    f.CurriculumCode,
                    f.SubjectCode,
                    schools.Suppressed ? null : f.MasteryRatePercent,
                    schools.Suppressed ? null : f.CoveragePercent,
                    schools);
            })
            .ToList();

        return new CurriculumEffectivenessComparisonResponse(asOf, regions);
    }

    public async Task<InterventionImpactResponse> GetInterventionImpactAsync(
        CancellationToken cancellationToken = default)
    {
        var facts = await db.RegionalInterventionImpactFacts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var asOf = facts.Count == 0
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : facts.Max(f => f.AsOfDate);

        var regions = facts
            .OrderBy(f => f.RegionCode)
            .ThenBy(f => f.InterventionType)
            .Select(f =>
            {
                var total = CountCell.FromCount(f.TotalCount, MinimumGroupSize);
                var successful = total.Suppressed
                    ? CountCell.Hidden()
                    : CountCell.FromCount(f.SuccessfulCount, MinimumGroupSize);
                var anySuppressed = total.Suppressed || successful.Suppressed;
                decimal? successRate = null;
                decimal? averageGrowth = null;
                if (!anySuppressed)
                {
                    successRate = f.TotalCount == 0
                        ? 0m
                        : decimal.Round(100m * f.SuccessfulCount / f.TotalCount, 2);
                    averageGrowth = f.AverageGrowthPercent;
                }

                return new RegionInterventionImpact(
                    f.RegionCode,
                    f.InterventionType,
                    total,
                    successful,
                    successRate,
                    averageGrowth);
            })
            .ToList();

        return new InterventionImpactResponse(asOf, regions);
    }
}
