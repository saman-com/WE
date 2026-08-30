using EiService.Application;

namespace EiService.Tests;

public class ClassInsightsAggregationEngineTests
{
    private readonly ClassInsightsAggregationEngine _engine = new();

    [Fact]
    public void Aggregate_SampleClassData_ProducesMasteryDistributionAcrossMicroSkills()
    {
        var microSkillA = Guid.CreateVersion7();
        var microSkillB = Guid.CreateVersion7();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();

        var input = new ClassInsightsInput(
            organisationId,
            classId,
            ["student-1", "student-2", "student-3"],
            [
                CreateStudent(
                    "student-1",
                    [CreateMastery(microSkillA, "Mastered", "Aggregated 2 evidence sources with weighted average 4.5/5.")],
                    [],
                    []),
                CreateStudent(
                    "student-2",
                    [CreateMastery(microSkillA, "Developing", "Aggregated 1 evidence source with weighted average 2/5.")],
                    [],
                    []),
                CreateStudent("student-3", [], [], [])
            ]);

        var result = _engine.Aggregate(input);

        var distribution = Assert.Single(result.MasteryDistribution, item => item.MicroSkillId == microSkillA);
        Assert.Equal(3, distribution.TotalStudents);
        Assert.Equal(1, distribution.LevelCounts["Mastered"]);
        Assert.Equal(1, distribution.LevelCounts["Developing"]);
        Assert.Equal(1, distribution.LevelCounts["NotStarted"]);
        Assert.Contains("student-1", distribution.Explanation);
        Assert.Contains("student-2", distribution.Explanation);
    }

    [Fact]
    public void Aggregate_SampleClassData_ListsActiveGapsWithSeverityAndStudentNames()
    {
        var microSkill = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();
        var gapId = Guid.CreateVersion7();

        var input = new ClassInsightsInput(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            ["student-1", "student-2"],
            [
                CreateStudent(
                    "student-1",
                    [],
                    [CreateGap(gapId, evidenceId, microSkill, "High", "High", "Expected mastery: Mastered; demonstrated Struggling.")],
                    []),
                CreateStudent(
                    "student-2",
                    [],
                    [CreateGap(Guid.CreateVersion7(), Guid.CreateVersion7(), microSkill, "Medium", "Medium", "Gap on shared micro-skill.")],
                    [])
            ]);

        var result = _engine.Aggregate(input);

        Assert.Equal(2, result.ActiveLearningGaps.Count);
        var highGap = result.ActiveLearningGaps[0];
        Assert.Equal("High", highGap.Severity);
        Assert.Equal("student-1", highGap.StudentUserId);
        Assert.Equal(evidenceId, highGap.EvidenceId);
        Assert.Contains("Expected mastery", highGap.Explanation);
    }

    [Fact]
    public void Aggregate_SampleClassData_ProducesRecentDiagnosticTrendsWithLinkedEvidence()
    {
        var microSkill = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();
        var createdAt = new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero);

        var input = new ClassInsightsInput(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            ["student-1", "student-2"],
            [
                CreateStudent(
                    "student-1",
                    [],
                    [],
                    [CreateDiagnostic(evidenceId, microSkill, "Struggling", "Mark below expected threshold.", createdAt)]),
                CreateStudent(
                    "student-2",
                    [],
                    [],
                    [CreateDiagnostic(Guid.CreateVersion7(), microSkill, "Struggling", "Repeated misconception.", createdAt.AddHours(-1))])
            ]);

        var result = _engine.Aggregate(input);

        var trend = Assert.Single(result.RecentDiagnosticTrends);
        Assert.Equal(microSkill, trend.MicroSkillId);
        Assert.Equal("Struggling", trend.Status);
        Assert.Equal(2, trend.OccurrenceCount);
        Assert.Equal(createdAt, trend.LatestAt);
        Assert.Equal(evidenceId, trend.EvidenceId);
        Assert.Contains("Mark below expected threshold", trend.Explanation);
    }

    [Fact]
    public void Aggregate_SampleClassData_IdentifiesStudentsNeedingAttention()
    {
        var microSkill = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();

        var input = new ClassInsightsInput(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            ["student-gap", "student-diagnostic", "student-ok"],
            [
                CreateStudent(
                    "student-gap",
                    [],
                    [CreateGap(Guid.CreateVersion7(), evidenceId, microSkill, "High", "High", "High severity gap.")],
                    []),
                CreateStudent(
                    "student-diagnostic",
                    [],
                    [],
                    [CreateDiagnostic(evidenceId, microSkill, "Struggling", "Needs support.", DateTimeOffset.UtcNow)]),
                CreateStudent("student-ok", [CreateMastery(microSkill, "Mastered", "Strong performance.")], [], [])
            ]);

        var result = _engine.Aggregate(input);

        Assert.Equal(2, result.StudentsNeedingAttention.Count);
        Assert.Contains(result.StudentsNeedingAttention, item => item.StudentUserId == "student-gap");
        Assert.Contains(result.StudentsNeedingAttention, item => item.StudentUserId == "student-diagnostic");
        Assert.DoesNotContain(result.StudentsNeedingAttention, item => item.StudentUserId == "student-ok");
    }

    [Fact]
    public void Aggregate_ProducesDeterministicResults()
    {
        var input = BuildSampleClassInput();

        var first = _engine.Aggregate(input);
        var second = _engine.Aggregate(input);

        Assert.Equal(first.MasteryDistribution.Count, second.MasteryDistribution.Count);
        Assert.Equal(first.ActiveLearningGaps.Count, second.ActiveLearningGaps.Count);
        Assert.Equal(first.RecentDiagnosticTrends.Count, second.RecentDiagnosticTrends.Count);
        Assert.Equal(first.StudentsNeedingAttention.Count, second.StudentsNeedingAttention.Count);
        Assert.Equal(
            first.ActiveLearningGaps.Select(gap => gap.StudentUserId),
            second.ActiveLearningGaps.Select(gap => gap.StudentUserId));
    }

    private static ClassInsightsInput BuildSampleClassInput()
    {
        var microSkill = Guid.CreateVersion7();
        var createdAt = new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
        return new ClassInsightsInput(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            ["student-1"],
            [
                CreateStudent(
                    "student-1",
                    [CreateMastery(microSkill, "Proficient", "Single evidence source.")],
                    [CreateGap(Guid.CreateVersion7(), Guid.CreateVersion7(), microSkill, "Low", "Low", "Minor gap.")],
                    [CreateDiagnostic(Guid.CreateVersion7(), microSkill, "Developing", "Partial understanding.", createdAt)])
            ]);
    }

    private static StudentEiSnapshot CreateStudent(
        string studentUserId,
        IReadOnlyList<StudentMasterySnapshot> masteryRecords,
        IReadOnlyList<StudentGapSnapshot> gaps,
        IReadOnlyList<StudentDiagnosticSnapshot> diagnostics) =>
        new(studentUserId, masteryRecords, gaps, diagnostics);

    private static StudentMasterySnapshot CreateMastery(
        Guid microSkillId,
        string masteryLevel,
        string explanation) =>
        new(microSkillId, masteryLevel, explanation);

    private static StudentGapSnapshot CreateGap(
        Guid id,
        Guid evidenceId,
        Guid microSkillId,
        string severity,
        string urgency,
        string explanation) =>
        new(id, evidenceId, microSkillId, severity, urgency, explanation);

    private static StudentDiagnosticSnapshot CreateDiagnostic(
        Guid evidenceId,
        Guid microSkillId,
        string status,
        string reason,
        DateTimeOffset createdAt) =>
        new(evidenceId, microSkillId, status, reason, createdAt);
}
