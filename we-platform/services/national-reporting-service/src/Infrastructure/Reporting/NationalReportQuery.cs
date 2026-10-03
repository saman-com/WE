using Microsoft.EntityFrameworkCore;
using NationalReportingService.Application;
using NationalReportingService.Infrastructure.Data;

namespace NationalReportingService.Infrastructure.Reporting;

public sealed class NationalReportQuery(NationalReportingDbContext db) : INationalReportQuery
{
    public async Task<NationalEnrollmentResponse> GetEnrollmentAsync(
        CancellationToken cancellationToken = default)
    {
        var facts = await db.RegionalEnrollmentFacts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var asOf = facts.Count == 0
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : facts.Max(f => f.AsOfDate);

        var regions = facts
            .GroupBy(f => f.RegionCode)
            .OrderBy(g => g.Key)
            .Select(g => new RegionEnrollmentMetric(
                g.Key,
                g.Select(f => f.SchoolCode).Distinct().Count(),
                g.Sum(f => f.StudentCount),
                g.Sum(f => f.TeacherCount)))
            .ToList();

        return new NationalEnrollmentResponse(
            regions.Sum(r => r.SchoolCount),
            regions.Sum(r => r.StudentCount),
            regions.Sum(r => r.TeacherCount),
            asOf,
            regions);
    }

    public async Task<NationalMasteryBenchmarksResponse> GetMasteryBenchmarksAsync(
        CancellationToken cancellationToken = default)
    {
        var facts = await db.RegionalMasteryFacts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var asOf = facts.Count == 0
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : facts.Max(f => f.AsOfDate);

        var regions = facts
            .GroupBy(f => f.RegionCode)
            .OrderBy(g => g.Key)
            .Select(g => new RegionMasteryBenchmark(
                g.Key,
                g.OrderBy(f => f.SubjectCode)
                    .Select(f => new NationalMasteryBenchmark(
                        f.SubjectCode,
                        f.AverageMasteryPercent,
                        f.MasteredSharePercent,
                        f.SampleSize))
                    .ToList()))
            .ToList();

        var national = facts
            .GroupBy(f => f.SubjectCode)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var sampleSize = g.Sum(f => f.SampleSize);
                var average = sampleSize == 0
                    ? 0m
                    : g.Sum(f => f.AverageMasteryPercent * f.SampleSize) / sampleSize;
                var masteredShare = sampleSize == 0
                    ? 0m
                    : g.Sum(f => f.MasteredSharePercent * f.SampleSize) / sampleSize;
                return new NationalMasteryBenchmark(
                    g.Key,
                    decimal.Round(average, 2),
                    decimal.Round(masteredShare, 2),
                    sampleSize);
            })
            .ToList();

        return new NationalMasteryBenchmarksResponse(asOf, national, regions);
    }

    public async Task<NationalCurriculumCoverageResponse> GetCurriculumCoverageAsync(
        CancellationToken cancellationToken = default)
    {
        var facts = await db.RegionalCoverageFacts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var asOf = facts.Count == 0
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : facts.Max(f => f.AsOfDate);

        var regions = facts
            .GroupBy(f => f.RegionCode)
            .OrderBy(g => g.Key)
            .Select(g => new RegionCoverageMetric(
                g.Key,
                g.OrderBy(f => f.CurriculumCode)
                    .Select(f => new CurriculumCoverageMetric(
                        f.CurriculumCode,
                        f.CoveredObjectivePercent,
                        f.SchoolsReporting))
                    .ToList()))
            .ToList();

        var national = facts
            .GroupBy(f => f.CurriculumCode)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var schoolsReporting = g.Sum(f => f.SchoolsReporting);
                var covered = schoolsReporting == 0
                    ? 0m
                    : g.Sum(f => f.CoveredObjectivePercent * f.SchoolsReporting) / schoolsReporting;
                return new CurriculumCoverageMetric(
                    g.Key,
                    decimal.Round(covered, 2),
                    schoolsReporting);
            })
            .ToList();

        return new NationalCurriculumCoverageResponse(asOf, national, regions);
    }
}
