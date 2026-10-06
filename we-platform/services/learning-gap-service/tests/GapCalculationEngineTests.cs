using LearningGapService.Application;

namespace LearningGapService.Tests;

public class GapCalculationEngineTests
{
    private readonly GapCalculationEngine _engine = new();

    [Fact]
    public void CalculateFromDiagnostic_MasteredDiagnostic_ProducesNoGap()
    {
        var diagnostic = CreateDiagnostic("Mastered", 5);

        var gap = _engine.CalculateFromDiagnostic(diagnostic);

        Assert.Null(gap);
    }

    [Theory]
    [InlineData("Struggling", 1, "High", "High")]
    [InlineData("Developing", 2, "Medium", "Medium")]
    [InlineData("Developing", 3, "Low", "Low")]
    public void CalculateFromDiagnostic_NonMasteredDiagnostic_ProducesGapWithSeverityAndUrgency(
        string status,
        decimal mark,
        string expectedSeverity,
        string expectedUrgency)
    {
        var microSkillId = Guid.CreateVersion7();
        var diagnostic = CreateDiagnostic(status, mark, microSkillId);

        var gap = _engine.CalculateFromDiagnostic(diagnostic);

        Assert.NotNull(gap);
        Assert.Equal(microSkillId, gap.MicroSkillId);
        Assert.Equal("Mastered", gap.ExpectedMastery);
        Assert.Equal(status, gap.ActualMastery);
        Assert.Equal(expectedSeverity, gap.Severity);
        Assert.Equal(expectedUrgency, gap.Urgency);
        Assert.Contains("Expected mastery: Mastered", gap.Explanation);
        Assert.Contains(status, gap.Explanation);
        Assert.Contains("This micro-skill", gap.Explanation);
        Assert.DoesNotContain(microSkillId.ToString(), gap.Explanation);
        Assert.DoesNotContain(diagnostic.EvidenceId.ToString(), gap.Explanation);
    }

    [Fact]
    public void CalculateFromDiagnostics_ProducesDeterministicGaps()
    {
        var diagnostics = new[]
        {
            CreateDiagnostic("Mastered", 5),
            CreateDiagnostic("Struggling", 1),
            CreateDiagnostic("Developing", 3)
        };

        var first = _engine.CalculateFromDiagnostics(diagnostics);
        var second = _engine.CalculateFromDiagnostics(diagnostics);

        Assert.Equal(first, second);
        Assert.Equal(2, first.Count);
    }

    private static MicroSkillDiagnosticInput CreateDiagnostic(
        string status,
        decimal mark,
        Guid? microSkillId = null)
    {
        var evidenceId = Guid.CreateVersion7();
        var skillId = microSkillId ?? Guid.CreateVersion7();
        return new MicroSkillDiagnosticInput(
            evidenceId,
            Guid.CreateVersion7(),
            skillId,
            status,
            mark,
            $"Marked {mark}/5. Classification: {status}.");
    }
}
