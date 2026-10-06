namespace OrganisationService.Application;

public sealed record ApiErrorResponse(string Code);

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

public sealed record StudentWorkspaceAssessmentSummary(
    Guid Id,
    Guid OrganisationId,
    Guid ClassId,
    string ClassName,
    string Title,
    DateTimeOffset? DueAt,
    IReadOnlyList<Guid> LearningObjectiveIds,
    bool HasSubmitted,
    DateTimeOffset? SubmittedAt);

public sealed record StudentWorkspaceFeedbackMark(
    Guid MicroSkillId,
    decimal Mark,
    string Feedback);

public sealed record StudentWorkspaceFeedbackSummary(
    Guid EvidenceId,
    Guid AssessmentId,
    string Title,
    DateTimeOffset ApprovedAt,
    IReadOnlyList<StudentWorkspaceFeedbackMark> MicroSkillMarks);

public sealed record StudentWorkspaceTimelineEntry(
    Guid Id,
    string Title,
    DateTimeOffset RecordedAt);

public sealed record StudentWorkspaceResponse(
    string StudentUserId,
    IReadOnlyList<StudentWorkspaceAssessmentSummary> Assessments,
    IReadOnlyList<StudentWorkspaceFeedbackSummary> Feedback,
    IReadOnlyList<StudentWorkspaceTimelineEntry> Timeline);

public sealed record LinkParentStudentRequest(string StudentUserId);

public sealed record ParentChildLinkResponse(string ParentUserId, string StudentUserId);

public sealed record ParentMasterySummary(Guid MicroSkillId, string MasteryLevel);

public sealed record ParentAssessmentSummary(
    Guid Id,
    string ClassName,
    string Title,
    DateTimeOffset? DueAt,
    bool HasSubmitted,
    DateTimeOffset? SubmittedAt);

public sealed record ParentInterventionSummary(
    Guid Id,
    string Summary,
    string Status,
    DateTimeOffset? PlannedStartAt,
    DateTimeOffset? PlannedEndAt);

public sealed record ParentChildProgressResponse(
    string StudentUserId,
    IReadOnlyList<ParentMasterySummary> Mastery,
    IReadOnlyList<StudentWorkspaceFeedbackSummary> Feedback,
    IReadOnlyList<ParentAssessmentSummary> Assessments,
    IReadOnlyList<ParentInterventionSummary> ActiveInterventions);
