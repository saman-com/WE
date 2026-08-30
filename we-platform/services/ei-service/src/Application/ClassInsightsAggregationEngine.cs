namespace EiService.Application;

public sealed class ClassInsightsAggregationEngine : IClassInsightsAggregationEngine
{
    private static readonly string[] MasteryLevels = ["NotStarted", "Developing", "Proficient", "Mastered"];
    private static readonly Dictionary<string, int> SeverityRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["High"] = 0,
        ["Medium"] = 1,
        ["Low"] = 2
    };

    public ClassEiInsightsResponse Aggregate(ClassInsightsInput input)
    {
        var masteryDistribution = BuildMasteryDistribution(input.StudentUserIds, input.Students);
        var activeLearningGaps = BuildActiveLearningGaps(input.Students);
        var recentDiagnosticTrends = BuildRecentDiagnosticTrends(input.Students);
        var studentsNeedingAttention = BuildStudentsNeedingAttention(input.Students);

        return new ClassEiInsightsResponse(
            input.OrganisationId,
            input.ClassId,
            masteryDistribution,
            activeLearningGaps,
            recentDiagnosticTrends,
            studentsNeedingAttention);
    }

    private static IReadOnlyList<ClassMicroSkillMasteryDistribution> BuildMasteryDistribution(
        IReadOnlyList<string> studentUserIds,
        IReadOnlyList<StudentEiSnapshot> students)
    {
        var masteryByStudent = students.ToDictionary(student => student.StudentUserId);
        var microSkillIds = students
            .SelectMany(student => student.MasteryRecords)
            .Select(record => record.MicroSkillId)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        return microSkillIds
            .Select(microSkillId =>
            {
                var levelCounts = MasteryLevels.ToDictionary(level => level, _ => 0);
                var explanations = new List<string>();
                var linkedEvidence = new List<Guid>();

                foreach (var studentUserId in studentUserIds)
                {
                    if (!masteryByStudent.TryGetValue(studentUserId, out var student))
                    {
                        levelCounts["NotStarted"]++;
                        continue;
                    }

                    var record = student.MasteryRecords.FirstOrDefault(item => item.MicroSkillId == microSkillId);
                    if (record is null)
                    {
                        levelCounts["NotStarted"]++;
                        continue;
                    }

                    levelCounts[record.MasteryLevel] = levelCounts.GetValueOrDefault(record.MasteryLevel) + 1;
                    explanations.Add($"{studentUserId}: {record.Explanation}");
                    linkedEvidence.AddRange(
                        student.Gaps
                            .Where(gap => gap.MicroSkillId == microSkillId)
                            .Select(gap => gap.EvidenceId));
                    linkedEvidence.AddRange(
                        student.Diagnostics
                            .Where(diagnostic => diagnostic.MicroSkillId == microSkillId)
                            .Select(diagnostic => diagnostic.EvidenceId));
                }

                return new ClassMicroSkillMasteryDistribution(
                    microSkillId,
                    levelCounts,
                    studentUserIds.Count,
                    string.Join(" ", explanations),
                    linkedEvidence.Distinct().ToList());
            })
            .ToList();
    }

    private static IReadOnlyList<ClassActiveLearningGap> BuildActiveLearningGaps(
        IReadOnlyList<StudentEiSnapshot> students) =>
        students
            .SelectMany(student => student.Gaps.Select(gap => new ClassActiveLearningGap(
                gap.Id,
                gap.MicroSkillId,
                gap.Severity,
                gap.Urgency,
                student.StudentUserId,
                gap.Explanation,
                gap.EvidenceId)))
            .OrderBy(gap => SeverityRank.GetValueOrDefault(gap.Severity, 99))
            .ThenByDescending(gap => gap.Urgency, StringComparer.OrdinalIgnoreCase)
            .ThenBy(gap => gap.StudentUserId, StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyList<ClassDiagnosticTrend> BuildRecentDiagnosticTrends(
        IReadOnlyList<StudentEiSnapshot> students)
    {
        var diagnostics = students
            .SelectMany(student => student.Diagnostics)
            .ToList();

        return diagnostics
            .GroupBy(diagnostic => (diagnostic.MicroSkillId, diagnostic.Status))
            .Select(group =>
            {
                var latest = group.OrderByDescending(item => item.CreatedAt).First();
                return new ClassDiagnosticTrend(
                    group.Key.MicroSkillId,
                    group.Key.Status,
                    group.Count(),
                    latest.CreatedAt,
                    latest.Reason,
                    latest.EvidenceId);
            })
            .OrderByDescending(trend => trend.LatestAt)
            .ThenBy(trend => trend.MicroSkillId)
            .ToList();
    }

    private static IReadOnlyList<StudentNeedingAttention> BuildStudentsNeedingAttention(
        IReadOnlyList<StudentEiSnapshot> students) =>
        students
            .Select(student =>
            {
                var highGap = student.Gaps
                    .OrderBy(gap => SeverityRank.GetValueOrDefault(gap.Severity, 99))
                    .FirstOrDefault(gap => string.Equals(gap.Severity, "High", StringComparison.OrdinalIgnoreCase));
                if (highGap is not null)
                {
                    return new StudentNeedingAttention(
                        student.StudentUserId,
                        "High severity learning gap",
                        highGap.Explanation,
                        highGap.EvidenceId);
                }

                var strugglingDiagnostic = student.Diagnostics
                    .FirstOrDefault(diagnostic =>
                        string.Equals(diagnostic.Status, "Struggling", StringComparison.OrdinalIgnoreCase));
                if (strugglingDiagnostic is not null)
                {
                    return new StudentNeedingAttention(
                        student.StudentUserId,
                        "Struggling diagnostic trend",
                        strugglingDiagnostic.Reason,
                        strugglingDiagnostic.EvidenceId);
                }

                return null;
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => item.StudentUserId, StringComparer.Ordinal)
            .ToList();
}
