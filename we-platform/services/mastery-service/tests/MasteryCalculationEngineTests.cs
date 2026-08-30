using MasteryService.Application;
using MasteryService.Domain;

namespace MasteryService.Tests;

public class MasteryCalculationEngineTests
{
    private static readonly MasteryThresholds DefaultThresholds = new();

    private readonly MasteryCalculationEngine _engine = new(DefaultThresholds);

    [Fact]
    public void Calculate_NoEvidence_ReturnsNotStarted()
    {
        var result = _engine.Calculate([]);

        Assert.Equal(MasteryLevel.NotStarted, result.MasteryLevel);
        Assert.Equal(0, result.EvidenceCount);
        Assert.Equal(0m, result.WeightedAverage);
        Assert.Contains("No approved evidence", result.Explanation);
    }

    [Fact]
    public void Calculate_SingleLowMark_ReturnsDeveloping()
    {
        var result = _engine.Calculate([new EvidenceMarkInput(2m)]);

        Assert.Equal(MasteryLevel.Developing, result.MasteryLevel);
        Assert.Equal(1, result.EvidenceCount);
        Assert.Equal(2m, result.WeightedAverage);
    }

    [Fact]
    public void Calculate_SingleHighMark_ReturnsProficientUntilSecondSource()
    {
        var result = _engine.Calculate([new EvidenceMarkInput(5m)]);

        Assert.Equal(MasteryLevel.Proficient, result.MasteryLevel);
        Assert.Equal(1, result.EvidenceCount);
        Assert.Equal(5m, result.WeightedAverage);
    }

    [Fact]
    public void Calculate_TwoHighMarksFromMultipleSources_ReturnsMastered()
    {
        var result = _engine.Calculate(
        [
            new EvidenceMarkInput(4m),
            new EvidenceMarkInput(5m)
        ]);

        Assert.Equal(MasteryLevel.Mastered, result.MasteryLevel);
        Assert.Equal(2, result.EvidenceCount);
        Assert.Equal(4.5m, result.WeightedAverage);
        Assert.Contains("2 evidence sources", result.Explanation);
    }

    [Fact]
    public void Calculate_MultipleMixedMarks_AggregatesNotLatestOnly()
    {
        var result = _engine.Calculate(
        [
            new EvidenceMarkInput(1m),
            new EvidenceMarkInput(5m)
        ]);

        Assert.Equal(MasteryLevel.Proficient, result.MasteryLevel);
        Assert.Equal(3m, result.WeightedAverage);
    }

    [Fact]
    public void Calculate_WeightedEvidence_AppliesSourceWeights()
    {
        var thresholds = new MasteryThresholds { DefaultEvidenceWeight = 1m };
        var engine = new MasteryCalculationEngine(thresholds);

        var result = engine.Calculate(
        [
            new EvidenceMarkInput(2m, 1m),
            new EvidenceMarkInput(5m, 3m)
        ]);

        Assert.Equal(4.25m, result.WeightedAverage);
        Assert.Equal(MasteryLevel.Mastered, result.MasteryLevel);
    }

    [Fact]
    public void Calculate_ProducesDeterministicResults()
    {
        var evidence =
            new[]
            {
                new EvidenceMarkInput(3m),
                new EvidenceMarkInput(4m),
                new EvidenceMarkInput(2m)
            };

        var first = _engine.Calculate(evidence);
        var second = _engine.Calculate(evidence);

        Assert.Equal(first, second);
        Assert.Equal(MasteryLevel.Proficient, first.MasteryLevel);
    }
}
