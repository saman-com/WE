namespace OrganisationService.Application;

public sealed record AssignSchoolLeaderRequest(string UserId);

public sealed record SchoolLeaderResponse(string UserId);

public sealed record LeadershipKpiSummary(
    int TotalStudents,
    int TotalClasses,
    int ActiveInterventions,
    int ActiveLearningGaps,
    int StudentsNeedingAttention,
    decimal AssessmentCompletionRate,
    IReadOnlyDictionary<string, int> MasteryLevelCounts);

public sealed record YearLevelDashboardSummary(
    Guid YearLevelId,
    string YearLevelName,
    int ClassCount,
    int StudentCount,
    int ActiveInterventions,
    int ActiveLearningGaps);

public sealed record ClassComparisonSummary(
    Guid ClassId,
    string ClassName,
    Guid YearLevelId,
    string YearLevelName,
    int StudentCount,
    int ActiveInterventions,
    int ActiveLearningGaps,
    int StudentsNeedingAttention,
    decimal AssessmentCompletionRate);

public sealed record LeadershipDashboardResponse(
    Guid OrganisationId,
    string OrganisationName,
    LeadershipKpiSummary Kpis,
    IReadOnlyList<YearLevelDashboardSummary> YearLevels,
    IReadOnlyList<ClassComparisonSummary> ClassComparisons);

public sealed record YearLevelLeadershipDashboardResponse(
    Guid OrganisationId,
    Guid YearLevelId,
    string YearLevelName,
    LeadershipKpiSummary Kpis,
    IReadOnlyList<ClassComparisonSummary> ClassComparisons);

public sealed record ClassLeadershipSummaryResponse(
    ClassResponse Class,
    IReadOnlyList<ClassMemberResponse> Students,
    IReadOnlyList<ClassDashboardAssessmentSummary> RecentAssessments,
    int ActiveInterventions,
    ClassEiInsightsData? EiInsights);

public sealed record ClassMasteryDistributionData(
    Guid MicroSkillId,
    IReadOnlyDictionary<string, int> LevelCounts,
    int TotalStudents,
    string Explanation,
    IReadOnlyList<Guid> LinkedEvidenceIds);

public sealed record ClassActiveGapData(
    Guid GapId,
    Guid MicroSkillId,
    string Severity,
    string Urgency,
    string StudentUserId,
    string Explanation,
    Guid EvidenceId);

public sealed record StudentNeedingAttentionData(
    string StudentUserId,
    string Reason,
    string Explanation,
    Guid? EvidenceId);

public sealed record ClassEiInsightsData(
    Guid OrganisationId,
    Guid ClassId,
    IReadOnlyList<ClassMasteryDistributionData> MasteryDistribution,
    IReadOnlyList<ClassActiveGapData> ActiveLearningGaps,
    IReadOnlyList<object> RecentDiagnosticTrends,
    IReadOnlyList<StudentNeedingAttentionData> StudentsNeedingAttention);

public interface IEiInsightsClient
{
    Task<ClassEiInsightsData?> GetClassInsightsAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public sealed record ClassAggregationInput(
    Guid ClassId,
    string ClassName,
    Guid YearLevelId,
    string YearLevelName,
    IReadOnlyList<string> StudentUserIds,
    IReadOnlyList<AssessmentSummaryData> Assessments,
    int ActiveInterventions,
    ClassEiInsightsData? EiInsights);

public sealed record ClassAggregationResult(
    Guid ClassId,
    string ClassName,
    Guid YearLevelId,
    string YearLevelName,
    int StudentCount,
    int ActiveInterventions,
    int ActiveLearningGaps,
    int StudentsNeedingAttention,
    decimal AssessmentCompletionRate,
    IReadOnlyDictionary<string, int> MasteryLevelCounts);

public static class LeadershipDashboardAggregator
{
    public static ClassAggregationResult AggregateClass(ClassAggregationInput input)
    {
        var studentCount = input.StudentUserIds.Count;
        var gaps = input.EiInsights?.ActiveLearningGaps.Count ?? 0;
        var needingAttention = input.EiInsights?.StudentsNeedingAttention.Count ?? 0;

        var masteryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (input.EiInsights is not null)
        {
            foreach (var distribution in input.EiInsights.MasteryDistribution)
            {
                foreach (var (level, count) in distribution.LevelCounts)
                {
                    masteryCounts[level] = masteryCounts.GetValueOrDefault(level) + count;
                }
            }
        }

        var assessmentCompletionRate = CalculateAssessmentCompletionRate(input.Assessments, studentCount);

        return new ClassAggregationResult(
            input.ClassId,
            input.ClassName,
            input.YearLevelId,
            input.YearLevelName,
            studentCount,
            input.ActiveInterventions,
            gaps,
            needingAttention,
            assessmentCompletionRate,
            masteryCounts);
    }

    public static LeadershipKpiSummary RollUpKpis(IReadOnlyList<ClassAggregationResult> classes)
    {
        var masteryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var schoolClass in classes)
        {
            foreach (var (level, count) in schoolClass.MasteryLevelCounts)
            {
                masteryCounts[level] = masteryCounts.GetValueOrDefault(level) + count;
            }
        }

        var totalStudents = classes.Sum(c => c.StudentCount);
        var avgCompletion = totalStudents > 0 && classes.Count > 0
            ? classes.Average(c => c.AssessmentCompletionRate)
            : 0m;

        return new LeadershipKpiSummary(
            totalStudents,
            classes.Count,
            classes.Sum(c => c.ActiveInterventions),
            classes.Sum(c => c.ActiveLearningGaps),
            classes.Sum(c => c.StudentsNeedingAttention),
            avgCompletion,
            masteryCounts);
    }

    public static IReadOnlyList<YearLevelDashboardSummary> RollUpYearLevels(
        IReadOnlyList<ClassAggregationResult> classes) =>
        classes
            .GroupBy(c => new { c.YearLevelId, c.YearLevelName })
            .Select(group => new YearLevelDashboardSummary(
                group.Key.YearLevelId,
                group.Key.YearLevelName,
                group.Count(),
                group.Sum(c => c.StudentCount),
                group.Sum(c => c.ActiveInterventions),
                group.Sum(c => c.ActiveLearningGaps)))
            .OrderBy(y => y.YearLevelName)
            .ToList();

    public static IReadOnlyList<ClassComparisonSummary> ToClassComparisons(
        IReadOnlyList<ClassAggregationResult> classes) =>
        classes
            .Select(c => new ClassComparisonSummary(
                c.ClassId,
                c.ClassName,
                c.YearLevelId,
                c.YearLevelName,
                c.StudentCount,
                c.ActiveInterventions,
                c.ActiveLearningGaps,
                c.StudentsNeedingAttention,
                c.AssessmentCompletionRate))
            .OrderBy(c => c.YearLevelName)
            .ThenBy(c => c.ClassName)
            .ToList();

    private static decimal CalculateAssessmentCompletionRate(
        IReadOnlyList<AssessmentSummaryData> assessments,
        int studentCount)
    {
        if (assessments.Count == 0 || studentCount == 0)
        {
            return 0m;
        }

        var expected = assessments.Count * studentCount;
        var submitted = assessments.Sum(a => a.SubmissionCount);
        return Math.Round((decimal)submitted / expected, 4);
    }
}
