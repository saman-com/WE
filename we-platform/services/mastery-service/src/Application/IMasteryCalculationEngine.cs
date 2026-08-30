namespace MasteryService.Application;

public interface IMasteryCalculationEngine
{
    CalculatedMastery Calculate(IReadOnlyList<EvidenceMarkInput> evidenceMarks);
}
