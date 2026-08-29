namespace OrganisationService.Application;

public sealed record CreateOrganisationRequest(string Name, string Code);

public sealed record UpdateOrganisationRequest(string Name, string Code);

public sealed record OrganisationResponse(Guid Id, string Name, string Code);

public sealed record CreateYearLevelRequest(string Name, int SortOrder);

public sealed record UpdateYearLevelRequest(string Name, int SortOrder);

public sealed record YearLevelResponse(Guid Id, Guid OrganisationId, string Name, int SortOrder);

public sealed record CreateClassRequest(string Name, string Code, Guid YearLevelId);

public sealed record UpdateClassRequest(string Name, string Code, Guid YearLevelId);

public sealed record ClassResponse(
    Guid Id,
    Guid OrganisationId,
    Guid YearLevelId,
    string Name,
    string Code,
    IReadOnlyList<string>? TeacherUserIds,
    IReadOnlyList<string>? StudentUserIds);

public sealed record AssignTeacherRequest(string UserId);

public sealed record EnrollStudentRequest(string UserId);

public sealed record ClassMemberResponse(string UserId);

public sealed record ClassDashboardStudentSummary(
    string StudentUserId,
    int EvidenceCount,
    DateTimeOffset? LatestActivityAt);

public sealed record ClassDashboardAssessmentSummary(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset? DueAt,
    int SubmissionCount,
    int ReviewedCount);

public sealed record ClassDashboardResponse(
    ClassResponse Class,
    IReadOnlyList<ClassDashboardStudentSummary> Roster,
    IReadOnlyList<ClassDashboardAssessmentSummary> RecentAssessments);
