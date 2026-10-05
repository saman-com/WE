using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NationalReportingService.Application;
using NationalReportingService.Infrastructure.Data;

namespace NationalReportingService.Infrastructure.Reporting;

public sealed class NationalReportQuery(
    NationalReportingDbContext db,
    IOptions<NationalReportingOptions> options) : INationalReportQuery
{
    private int MinimumGroupSize => options.Value.MinimumGroupSize;

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
            .Select(g =>
            {
                var studentCount = g.Sum(f => f.StudentCount);
                var teacherCount = g.Sum(f => f.TeacherCount);
                return new RegionEnrollmentMetric(
                    g.Key,
                    g.Select(f => f.SchoolCode).Distinct().Count(),
                    CountCell.FromCount(studentCount, MinimumGroupSize),
                    CountCell.FromCount(teacherCount, MinimumGroupSize));
            })
            .ToList();

        return new NationalEnrollmentResponse(
            regions.Sum(r => r.SchoolCount),
            CountCell.SumOrSuppress(regions.Select(r => r.StudentCount)),
            CountCell.SumOrSuppress(regions.Select(r => r.TeacherCount)),
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
                    .Select(f => ToMasteryBenchmark(
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
                var childCells = g
                    .Select(f => CountCell.FromCount(f.SampleSize, MinimumGroupSize))
                    .ToList();
                var sampleSize = CountCell.SumOrSuppress(childCells);
                if (sampleSize.Suppressed)
                {
                    return new NationalMasteryBenchmark(g.Key, null, null, sampleSize);
                }

                var rawSample = g.Sum(f => f.SampleSize);
                var average = rawSample == 0
                    ? 0m
                    : g.Sum(f => f.AverageMasteryPercent * f.SampleSize) / rawSample;
                var masteredShare = rawSample == 0
                    ? 0m
                    : g.Sum(f => f.MasteredSharePercent * f.SampleSize) / rawSample;
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
                    .Select(f => ToCoverageMetric(
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
                var childCells = g
                    .Select(f => CountCell.FromCount(f.SchoolsReporting, MinimumGroupSize))
                    .ToList();
                var schoolsReporting = CountCell.SumOrSuppress(childCells);
                if (schoolsReporting.Suppressed)
                {
                    return new CurriculumCoverageMetric(g.Key, null, schoolsReporting);
                }

                var rawSchools = g.Sum(f => f.SchoolsReporting);
                var covered = rawSchools == 0
                    ? 0m
                    : g.Sum(f => f.CoveredObjectivePercent * f.SchoolsReporting) / rawSchools;
                return new CurriculumCoverageMetric(
                    g.Key,
                    decimal.Round(covered, 2),
                    schoolsReporting);
            })
            .ToList();

        return new NationalCurriculumCoverageResponse(asOf, national, regions);
    }

    private NationalMasteryBenchmark ToMasteryBenchmark(
        string subjectCode,
        decimal averageMasteryPercent,
        decimal masteredSharePercent,
        int sampleSize)
    {
        var cell = CountCell.FromCount(sampleSize, MinimumGroupSize);
        if (cell.Suppressed)
        {
            return new NationalMasteryBenchmark(subjectCode, null, null, cell);
        }

        return new NationalMasteryBenchmark(
            subjectCode,
            averageMasteryPercent,
            masteredSharePercent,
            cell);
    }

    private CurriculumCoverageMetric ToCoverageMetric(
        string curriculumCode,
        decimal coveredObjectivePercent,
        int schoolsReporting)
    {
        var cell = CountCell.FromCount(schoolsReporting, MinimumGroupSize);
        if (cell.Suppressed)
        {
            return new CurriculumCoverageMetric(curriculumCode, null, cell);
        }

        return new CurriculumCoverageMetric(curriculumCode, coveredObjectivePercent, cell);
    }
}
