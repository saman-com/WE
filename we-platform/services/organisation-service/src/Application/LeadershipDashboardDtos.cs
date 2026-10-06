namespace OrganisationService.Application;

public sealed record AssignSchoolLeaderRequest(string UserId);

public sealed record SchoolLeaderResponse(string UserId);

public sealed record LeadershipKpiSummary(
    int TotalStudents,
    int TotalClasses,
    int ActiveInterventions,
    int? ActiveLearningGaps,
    int? StudentsNeedingAttention,
    decimal AssessmentCompletionRate,
    IReadOnlyDictionary<string, int> MasteryLevelCounts,
    string? StudentsNeedingAttentionReason = null);

public sealed record YearLevelDashboardSummary(
    Guid YearLevelId,
    string YearLevelName,
    int ClassCount,
    int StudentCount,
    int ActiveInterventions,
    int? ActiveLearningGaps);

public sealed record ClassComparisonSummary(
    Guid ClassId,
    string ClassName,
    Guid YearLevelId,
    string YearLevelName,
    int StudentCount,
    int ActiveInterventions,
    int? ActiveLearningGaps,
    int? StudentsNeedingAttention,
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

public sealed record LeadershipInterventionItem(
    Guid InterventionId,
    string StudentUserId,
    Guid LearningGapId,
    string AssignedTeacherUserId,
    string PlannedActions,
    string? Outcome,
    string Status,
    DateTimeOffset? PlannedStartAt,
    DateTimeOffset? PlannedEndAt,
    DateTimeOffset? ReviewAt,
    DateTimeOffset CreatedAt,
    Guid ClassId,
    string ClassName,
    Guid YearLevelId,
    string YearLevelName,
    string? GapSeverity);

public sealed record LeadershipInterventionMonitoringResponse(
    Guid OrganisationId,
    IReadOnlyList<LeadershipInterventionItem> Interventions);

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
    int? ActiveLearningGaps,
    int? StudentsNeedingAttention,
    decimal AssessmentCompletionRate,
    IReadOnlyDictionary<string, int> MasteryLevelCounts,
    string? AttentionReason);

public static class LeadershipDashboardAggregator
{
    public static ClassAggregationResult AggregateClass(ClassAggregationInput input)
    {
        var studentCount = input.StudentUserIds.Count;
        int? gaps = input.EiInsights is null ? null : input.EiInsights.ActiveLearningGaps.Count;
        int? needingAttention = input.EiInsights is null ? null : input.EiInsights.StudentsNeedingAttention.Count;

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
            masteryCounts,
            AttentionReason(input.EiInsights));
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
            SumOrUnavailable(classes.Select(c => c.ActiveLearningGaps)),
            SumOrUnavailable(classes.Select(c => c.StudentsNeedingAttention)),
            avgCompletion,
            masteryCounts,
            RollUpAttentionReason(classes));
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
                SumOrUnavailable(group.Select(c => c.ActiveLearningGaps))))
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

    private static string? AttentionReason(ClassEiInsightsData? insights)
    {
        if (insights is null)
        {
            return null;
        }

        if (insights.StudentsNeedingAttention.Any(item =>
                item.Reason.Contains("High severity", StringComparison.OrdinalIgnoreCase)
                || item.Reason.Contains("high-severity", StringComparison.OrdinalIgnoreCase)))
        {
            return "high-severity-gap";
        }

        if (insights.StudentsNeedingAttention.Any(item =>
                item.Reason.Contains("Struggling", StringComparison.OrdinalIgnoreCase)))
        {
            return "struggling-diagnostic";
        }

        return null;
    }

    private static string? RollUpAttentionReason(IReadOnlyList<ClassAggregationResult> classes)
    {
        if (classes.Any(schoolClass => schoolClass.AttentionReason == "high-severity-gap"))
        {
            return "high-severity-gap";
        }

        if (classes.Any(schoolClass => schoolClass.AttentionReason == "struggling-diagnostic"))
        {
            return "struggling-diagnostic";
        }

        return null;
    }

    private static int? SumOrUnavailable(IEnumerable<int?> values)
    {
        var sum = 0;
        foreach (var value in values)
        {
            if (value is null)
            {
                return null;
            }

            sum += value.Value;
        }

        return sum;
    }

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
