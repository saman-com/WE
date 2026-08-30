namespace MasteryService.Application;

public sealed record EvidenceMarkInput(decimal Mark, decimal? Weight = null);

public sealed record CalculatedMastery(
    string MasteryLevel,
    decimal WeightedAverage,
    decimal ConfidenceScore,
    int EvidenceCount,
    string Explanation);

public sealed record MasteryRecordResponse(
    Guid Id,
    Guid MicroSkillId,
    string MasteryLevel,
    decimal WeightedAverage,
    decimal ConfidenceScore,
    int EvidenceCount,
    string Explanation,
    DateTimeOffset CalculatedAt);

public sealed record StudentMasteryResponse(
    string StudentUserId,
    IReadOnlyList<MasteryRecordResponse> Records);

public sealed record ParentMasteryRecordResponse(Guid MicroSkillId, string MasteryLevel);

public sealed record ParentMasterySummaryResponse(
    string StudentUserId,
    IReadOnlyList<ParentMasteryRecordResponse> Records);
