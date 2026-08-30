namespace EiService.Application;

public sealed record StudentMasterySnapshot(
    Guid MicroSkillId,
    string MasteryLevel,
    string Explanation);

public sealed record StudentGapSnapshot(
    Guid Id,
    Guid EvidenceId,
    Guid MicroSkillId,
    string Severity,
    string Urgency,
    string Explanation);

public sealed record StudentDiagnosticSnapshot(
    Guid EvidenceId,
    Guid MicroSkillId,
    string Status,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record StudentEiSnapshot(
    string StudentUserId,
    IReadOnlyList<StudentMasterySnapshot> MasteryRecords,
    IReadOnlyList<StudentGapSnapshot> Gaps,
    IReadOnlyList<StudentDiagnosticSnapshot> Diagnostics);

public sealed record ClassInsightsInput(
    Guid OrganisationId,
    Guid ClassId,
    IReadOnlyList<string> StudentUserIds,
    IReadOnlyList<StudentEiSnapshot> Students);

public sealed record ClassMicroSkillMasteryDistribution(
    Guid MicroSkillId,
    IReadOnlyDictionary<string, int> LevelCounts,
    int TotalStudents,
    string Explanation,
    IReadOnlyList<Guid> LinkedEvidenceIds);

public sealed record ClassActiveLearningGap(
    Guid GapId,
    Guid MicroSkillId,
    string Severity,
    string Urgency,
    string StudentUserId,
    string Explanation,
    Guid EvidenceId);

public sealed record ClassDiagnosticTrend(
    Guid MicroSkillId,
    string Status,
    int OccurrenceCount,
    DateTimeOffset LatestAt,
    string Explanation,
    Guid EvidenceId);

public sealed record StudentNeedingAttention(
    string StudentUserId,
    string Reason,
    string Explanation,
    Guid? EvidenceId);

public sealed record ClassEiInsightsResponse(
    Guid OrganisationId,
    Guid ClassId,
    IReadOnlyList<ClassMicroSkillMasteryDistribution> MasteryDistribution,
    IReadOnlyList<ClassActiveLearningGap> ActiveLearningGaps,
    IReadOnlyList<ClassDiagnosticTrend> RecentDiagnosticTrends,
    IReadOnlyList<StudentNeedingAttention> StudentsNeedingAttention);
