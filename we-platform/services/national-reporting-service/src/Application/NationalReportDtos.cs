namespace NationalReportingService.Application;

public record RegionEnrollmentMetric(
    string RegionCode,
    int SchoolCount,
    CountCell StudentCount,
    CountCell TeacherCount);

public record NationalEnrollmentResponse(
    int TotalSchools,
    CountCell TotalStudents,
    CountCell TotalTeachers,
    DateOnly AsOfDate,
    IReadOnlyList<RegionEnrollmentMetric> Regions);

public record NationalMasteryBenchmark(
    string SubjectCode,
    decimal? AverageMasteryPercent,
    decimal? MasteredSharePercent,
    CountCell SampleSize);

public record RegionMasteryBenchmark(
    string RegionCode,
    IReadOnlyList<NationalMasteryBenchmark> Subjects);

public record NationalMasteryBenchmarksResponse(
    DateOnly AsOfDate,
    IReadOnlyList<NationalMasteryBenchmark> National,
    IReadOnlyList<RegionMasteryBenchmark> Regions);

public record CurriculumCoverageMetric(
    string CurriculumCode,
    decimal? CoveredObjectivePercent,
    CountCell SchoolsReporting);

public record RegionCoverageMetric(
    string RegionCode,
    IReadOnlyList<CurriculumCoverageMetric> Curricula);

public record NationalCurriculumCoverageResponse(
    DateOnly AsOfDate,
    IReadOnlyList<CurriculumCoverageMetric> National,
    IReadOnlyList<RegionCoverageMetric> Regions);
