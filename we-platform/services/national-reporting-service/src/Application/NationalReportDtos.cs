namespace NationalReportingService.Application;

public record RegionEnrollmentMetric(
    string RegionCode,
    int SchoolCount,
    int StudentCount,
    int TeacherCount);

public record NationalEnrollmentResponse(
    int TotalSchools,
    int TotalStudents,
    int TotalTeachers,
    DateOnly AsOfDate,
    IReadOnlyList<RegionEnrollmentMetric> Regions);

public record NationalMasteryBenchmark(
    string SubjectCode,
    decimal AverageMasteryPercent,
    decimal MasteredSharePercent,
    int SampleSize);

public record RegionMasteryBenchmark(
    string RegionCode,
    IReadOnlyList<NationalMasteryBenchmark> Subjects);

public record NationalMasteryBenchmarksResponse(
    DateOnly AsOfDate,
    IReadOnlyList<NationalMasteryBenchmark> National,
    IReadOnlyList<RegionMasteryBenchmark> Regions);

public record CurriculumCoverageMetric(
    string CurriculumCode,
    decimal CoveredObjectivePercent,
    int SchoolsReporting);

public record RegionCoverageMetric(
    string RegionCode,
    IReadOnlyList<CurriculumCoverageMetric> Curricula);

public record NationalCurriculumCoverageResponse(
    DateOnly AsOfDate,
    IReadOnlyList<CurriculumCoverageMetric> National,
    IReadOnlyList<RegionCoverageMetric> Regions);
