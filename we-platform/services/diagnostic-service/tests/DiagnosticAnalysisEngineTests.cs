using DiagnosticService.Application;
using WePlatform.Events;

namespace DiagnosticService.Tests;

public class DiagnosticAnalysisEngineTests
{
    private readonly DiagnosticAnalysisEngine _engine = new();

    [Theory]
    [InlineData(5, "Mastered")]
    [InlineData(4, "Mastered")]
    [InlineData(3, "Developing")]
    [InlineData(2, "Developing")]
    [InlineData(1, "Struggling")]
    public void ClassifyMark_ReturnsExpectedStatus(decimal mark, string expectedStatus)
    {
        Assert.Equal(expectedStatus, DiagnosticAnalysisEngine.ClassifyMark(mark));
    }

    [Fact]
    public void Analyze_ProducesDeterministicDiagnosticsFromEvidence()
    {
        var evidenceId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var evidence = CreateEvidence(evidenceId, microSkillId, 4, "Clear understanding.");

        var first = _engine.Analyze(evidence);
        var second = _engine.Analyze(evidence);

        Assert.Equal(first, second);
        var diagnostic = Assert.Single(first);
        Assert.Equal("Mastered", diagnostic.Status);
        Assert.Equal(microSkillId, diagnostic.MicroSkillId);
        Assert.Equal(evidenceId, diagnostic.EvidenceId);
        Assert.Equal(4, diagnostic.Mark);
        Assert.Contains(evidenceId.ToString(), diagnostic.Reason);
        Assert.Contains(microSkillId.ToString(), diagnostic.Reason);
        Assert.Contains("Clear understanding.", diagnostic.Reason);
    }

    [Fact]
    public void Analyze_ProducesOneDiagnosticPerMicroSkill()
    {
        var evidenceId = Guid.CreateVersion7();
        var masteredSkill = Guid.CreateVersion7();
        var developingSkill = Guid.CreateVersion7();
        var strugglingSkill = Guid.CreateVersion7();
        var evidence = new EvidenceCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            EvidenceCreated.CurrentVersion,
            evidenceId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            [
                new MicroSkillResult(masteredSkill, 5, "Excellent."),
                new MicroSkillResult(developingSkill, 3, "Partial understanding."),
                new MicroSkillResult(strugglingSkill, 1, "Needs support.")
            ]);

        var diagnostics = _engine.Analyze(evidence);

        Assert.Equal(3, diagnostics.Count);
        Assert.Contains(diagnostics, item => item.MicroSkillId == masteredSkill && item.Status == "Mastered");
        Assert.Contains(diagnostics, item => item.MicroSkillId == developingSkill && item.Status == "Developing");
        Assert.Contains(diagnostics, item => item.MicroSkillId == strugglingSkill && item.Status == "Struggling");
    }

    private static EvidenceCreated CreateEvidence(
        Guid evidenceId,
        Guid microSkillId,
        decimal mark,
        string feedback)
    {
        var eventId = Guid.CreateVersion7();
        return new EvidenceCreated(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            EvidenceCreated.CurrentVersion,
            evidenceId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            [new MicroSkillResult(microSkillId, mark, feedback)]);
    }
}
