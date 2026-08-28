namespace CurriculumService.Application;

public sealed record CreateCurriculumRequest(
    Guid OrganisationId,
    string Name,
    string Version,
    string? Status);

public sealed record UpdateCurriculumRequest(
    string Name,
    string Version,
    string Status);

public sealed record CurriculumResponse(
    Guid Id,
    Guid OrganisationId,
    string Name,
    string Version,
    string Status);

public sealed record CreateSubjectRequest(string Name, string Code, int SortOrder);

public sealed record UpdateSubjectRequest(string Name, string Code, int SortOrder);

public sealed record SubjectResponse(
    Guid Id,
    Guid CurriculumId,
    string Name,
    string Code,
    int SortOrder);

public sealed record CreateUnitRequest(string Name, int SortOrder);

public sealed record UpdateUnitRequest(string Name, int SortOrder);

public sealed record UnitResponse(
    Guid Id,
    Guid SubjectId,
    Guid CurriculumId,
    string Name,
    int SortOrder);

public sealed record CreateTopicRequest(string Name, int SortOrder);

public sealed record UpdateTopicRequest(string Name, int SortOrder);

public sealed record TopicResponse(
    Guid Id,
    Guid UnitId,
    Guid SubjectId,
    Guid CurriculumId,
    string Name,
    int SortOrder);

public sealed record UnitTreeResponse(
    Guid Id,
    string Name,
    int SortOrder,
    IReadOnlyList<TopicResponse> Topics);

public sealed record SubjectTreeResponse(
    Guid Id,
    string Name,
    string Code,
    int SortOrder,
    IReadOnlyList<UnitTreeResponse> Units);

public sealed record CurriculumTreeResponse(
    Guid Id,
    Guid OrganisationId,
    string Name,
    string Version,
    string Status,
    IReadOnlyList<SubjectTreeResponse> Subjects);
