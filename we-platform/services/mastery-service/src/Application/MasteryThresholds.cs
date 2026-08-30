namespace MasteryService.Application;

public sealed class MasteryThresholds
{
    public decimal ProficientMinAverage { get; init; } = 3.0m;
    public decimal MasteredMinAverage { get; init; } = 4.0m;
    public int MasteredMinEvidenceCount { get; init; } = 2;
    public decimal DefaultEvidenceWeight { get; init; } = 1.0m;
}
