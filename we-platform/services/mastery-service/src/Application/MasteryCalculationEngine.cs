using MasteryService.Domain;

namespace MasteryService.Application;

public sealed class MasteryCalculationEngine(MasteryThresholds thresholds) : IMasteryCalculationEngine
{
    public CalculatedMastery Calculate(IReadOnlyList<EvidenceMarkInput> evidenceMarks)
    {
        if (evidenceMarks.Count == 0)
        {
            return new CalculatedMastery(
                MasteryLevel.NotStarted,
                0m,
                0m,
                0,
                "No approved evidence recorded for this micro-skill.");
        }

        var totalWeight = 0m;
        var weightedSum = 0m;
        foreach (var evidence in evidenceMarks)
        {
            var weight = evidence.Weight ?? thresholds.DefaultEvidenceWeight;
            totalWeight += weight;
            weightedSum += evidence.Mark * weight;
        }

        var weightedAverage = Math.Round(weightedSum / totalWeight, 2, MidpointRounding.AwayFromZero);
        var evidenceCount = evidenceMarks.Count;
        var level = DetermineLevel(weightedAverage, evidenceCount);
        var confidence = CalculateConfidence(weightedAverage, evidenceCount);
        var explanation =
            $"Aggregated {evidenceCount} evidence source{(evidenceCount == 1 ? "" : "s")} " +
            $"with weighted average {weightedAverage}/5. " +
            $"Mastery level: {level}.";

        return new CalculatedMastery(level, weightedAverage, confidence, evidenceCount, explanation);
    }

    private string DetermineLevel(decimal weightedAverage, int evidenceCount)
    {
        if (weightedAverage < thresholds.ProficientMinAverage)
        {
            return MasteryLevel.Developing;
        }

        if (weightedAverage >= thresholds.MasteredMinAverage &&
            evidenceCount >= thresholds.MasteredMinEvidenceCount)
        {
            return MasteryLevel.Mastered;
        }

        return MasteryLevel.Proficient;
    }

    private decimal CalculateConfidence(decimal weightedAverage, int evidenceCount)
    {
        var evidenceFactor = Math.Min(1m, (decimal)evidenceCount / thresholds.MasteredMinEvidenceCount);
        var markFactor = weightedAverage / 5m;
        return Math.Round(evidenceFactor * markFactor, 2, MidpointRounding.AwayFromZero);
    }
}
