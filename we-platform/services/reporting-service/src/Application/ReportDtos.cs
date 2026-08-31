namespace ReportingService.Application;

public sealed record ClassMasteryDistributionSection(
    Guid MicroSkillId,
    IReadOnlyDictionary<string, int> LevelCounts,
    int TotalStudents,
    string Explanation,
    IReadOnlyList<Guid> LinkedEvidenceIds);

public sealed record ClassActiveGapSection(
    Guid GapId,
    Guid MicroSkillId,
    string Severity,
    string Urgency,
    string StudentUserId,
    string Explanation,
    Guid EvidenceId);

public sealed record AssessmentSummarySection(
    Guid AssessmentId,
    string Title,
    string Status,
    int SubmissionCount,
    int ReviewedCount,
    DateTimeOffset? DueAt);

public sealed record ClassProgressReportContent(
    Guid OrganisationId,
    Guid ClassId,
    string ClassName,
    IReadOnlyList<ClassMasteryDistributionSection> MasteryDistribution,
    IReadOnlyList<ClassActiveGapSection> ActiveLearningGaps,
    IReadOnlyList<AssessmentSummarySection> AssessmentSummary,
    DateTimeOffset GeneratedAt);

public sealed record LeadershipKpiSection(
    int TotalStudents,
    int TotalClasses,
    int ActiveInterventions,
    int ActiveLearningGaps,
    int StudentsNeedingAttention,
    decimal AssessmentCompletionRate,
    IReadOnlyDictionary<string, int> MasteryLevelCounts);

public sealed record YearLevelSummarySection(
    Guid YearLevelId,
    string YearLevelName,
    int ClassCount,
    int StudentCount,
    int ActiveInterventions,
    int ActiveLearningGaps);

public sealed record ClassComparisonSection(
    Guid ClassId,
    string ClassName,
    Guid YearLevelId,
    string YearLevelName,
    int StudentCount,
    int ActiveInterventions,
    int ActiveLearningGaps,
    int StudentsNeedingAttention,
    decimal AssessmentCompletionRate);

public sealed record SchoolSummaryReportContent(
    Guid OrganisationId,
    string OrganisationName,
    LeadershipKpiSection Kpis,
    IReadOnlyList<YearLevelSummarySection> YearLevels,
    IReadOnlyList<ClassComparisonSection> ClassComparisons,
    DateTimeOffset GeneratedAt);

public sealed record ReportResponse(
    Guid Id,
    string ReportType,
    Guid OrganisationId,
    Guid? ClassId,
    string RequestedByUserId,
    DateTimeOffset GeneratedAt,
    object Content);

public sealed record ClassEiInsightsData(
    Guid OrganisationId,
    Guid ClassId,
    IReadOnlyList<ClassMasteryDistributionSection> MasteryDistribution,
    IReadOnlyList<ClassActiveGapSection> ActiveLearningGaps);

public sealed record ClassDashboardData(
    Guid ClassId,
    string ClassName,
    IReadOnlyList<AssessmentSummarySection> RecentAssessments);

public sealed record SchoolSummaryData(
    Guid OrganisationId,
    string OrganisationName,
    LeadershipKpiSection Kpis,
    IReadOnlyList<YearLevelSummarySection> YearLevels,
    IReadOnlyList<ClassComparisonSection> ClassComparisons);

public interface IOrganisationAccessChecker
{
    Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<bool> SchoolLeaderCanViewOrganisationAsync(
        string schoolLeaderUserId,
        Guid organisationId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public interface IEiInsightsClient
{
    Task<ClassEiInsightsData?> GetClassInsightsAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public interface IClassDashboardClient
{
    Task<ClassDashboardData?> GetClassDashboardAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public interface ISchoolSummaryClient
{
    Task<SchoolSummaryData?> GetSchoolSummaryAsync(
        Guid organisationId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public interface IReportPdfExporter
{
    byte[] ExportClassProgressReport(ClassProgressReportContent content);
    byte[] ExportSchoolSummaryReport(SchoolSummaryReportContent content);
}

public interface IReportGenerator
{
    Task<ReportResponse?> GenerateClassProgressReportAsync(
        Guid organisationId,
        Guid classId,
        string requestedByUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<ReportResponse?> GenerateSchoolSummaryReportAsync(
        Guid organisationId,
        string requestedByUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<ReportResponse?> GetReportAsync(
        Guid reportId,
        string requestedByUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<byte[]?> ExportReportPdfAsync(
        Guid reportId,
        string requestedByUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public static class ClassProgressReportBuilder
{
    public static ClassProgressReportContent Build(
        ClassEiInsightsData insights,
        ClassDashboardData dashboard,
        DateTimeOffset generatedAt) =>
        new(
            insights.OrganisationId,
            insights.ClassId,
            dashboard.ClassName,
            insights.MasteryDistribution,
            insights.ActiveLearningGaps,
            dashboard.RecentAssessments,
            generatedAt);
}

public static class SchoolSummaryReportBuilder
{
    public static SchoolSummaryReportContent Build(
        SchoolSummaryData summary,
        DateTimeOffset generatedAt) =>
        new(
            summary.OrganisationId,
            summary.OrganisationName,
            summary.Kpis,
            summary.YearLevels,
            summary.ClassComparisons,
            generatedAt);
}
