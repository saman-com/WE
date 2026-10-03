namespace CurriculumService.Application;

public sealed record CreateCurriculumRequest(
    Guid OrganisationId,
    string Name,
    string Version,
    string? Status,
    string? RegionCode = null,
    string? Scope = null);

public sealed record UpdateCurriculumRequest(
    string Name,
    string Version,
    string Status);

public sealed record InheritCurriculumRequest(Guid OrganisationId);

public sealed record CurriculumResponse(
    Guid Id,
    Guid OrganisationId,
    string Name,
    string Version,
    string Status,
    string? RegionCode,
    string Scope,
    Guid? ParentCurriculumId);

public sealed record VariantCurriculumLinksResponse(
    Guid CurriculumId,
    IReadOnlyList<Guid> LearningObjectiveIds,
    IReadOnlyList<Guid> MicroSkillIds);

public sealed record CreateSubjectRequest(string Name, string Code, int SortOrder);

public sealed record UpdateSubjectRequest(string Name, string Code, int SortOrder);

public sealed record SubjectResponse(
    Guid Id,
    Guid CurriculumId,
    string Name,
    string Code,
    int SortOrder,
    Guid? SourceNodeId,
    bool IsOverridden);

public sealed record CreateUnitRequest(string Name, int SortOrder);

public sealed record UpdateUnitRequest(string Name, int SortOrder);

public sealed record UnitResponse(
    Guid Id,
    Guid SubjectId,
    Guid CurriculumId,
    string Name,
    int SortOrder,
    Guid? SourceNodeId,
    bool IsOverridden);

public sealed record CreateTopicRequest(string Name, int SortOrder);

public sealed record UpdateTopicRequest(string Name, int SortOrder);

public sealed record TopicResponse(
    Guid Id,
    Guid UnitId,
    Guid SubjectId,
    Guid CurriculumId,
    string Name,
    int SortOrder,
    Guid? SourceNodeId,
    bool IsOverridden);

public sealed record CreateLearningObjectiveRequest(string Title, int SortOrder);

public sealed record UpdateLearningObjectiveRequest(string Title, int SortOrder);

public sealed record LearningObjectiveResponse(
    Guid Id,
    Guid UnitId,
    Guid SubjectId,
    Guid CurriculumId,
    string Title,
    int SortOrder,
    Guid? SourceNodeId,
    bool IsOverridden);

public sealed record CreateMicroSkillRequest(string Name, int SortOrder);

public sealed record UpdateMicroSkillRequest(string Name, int SortOrder);

public sealed record MicroSkillResponse(
    Guid Id,
    Guid LearningObjectiveId,
    Guid UnitId,
    Guid SubjectId,
    Guid CurriculumId,
    string Name,
    int SortOrder,
    Guid? SourceNodeId,
    bool IsOverridden);

public sealed record MicroSkillTreeResponse(
    Guid Id,
    string Name,
    int SortOrder,
    Guid? SourceNodeId,
    bool IsOverridden);

public sealed record LearningObjectiveTreeResponse(
    Guid Id,
    string Title,
    int SortOrder,
    IReadOnlyList<MicroSkillTreeResponse> MicroSkills,
    Guid? SourceNodeId,
    bool IsOverridden);

public sealed record UnitTreeResponse(
    Guid Id,
    string Name,
    int SortOrder,
    IReadOnlyList<TopicResponse> Topics,
    IReadOnlyList<LearningObjectiveTreeResponse> LearningObjectives,
    Guid? SourceNodeId,
    bool IsOverridden);

public sealed record SubjectTreeResponse(
    Guid Id,
    string Name,
    string Code,
    int SortOrder,
    IReadOnlyList<UnitTreeResponse> Units,
    Guid? SourceNodeId,
    bool IsOverridden);

public sealed record CurriculumTreeResponse(
    Guid Id,
    Guid OrganisationId,
    string Name,
    string Version,
    string Status,
    IReadOnlyList<SubjectTreeResponse> Subjects,
    string? RegionCode,
    string Scope,
    Guid? ParentCurriculumId);
