namespace ReportingService.Infrastructure.Data.Edw;

using WePlatform.Tenancy;

public sealed class EdwDimTime : ITenantEntity
{
    public Guid TenantId { get; set; }
    public int DateKey { get; set; }
    public DateOnly CalendarDate { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}

public sealed class EdwEvidenceFact : ITenantEntity, IOrganisationTenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid EvidenceId { get; set; }
    public Guid OrganisationId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid SubmissionId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public Guid ClassId { get; set; }
    public string ApprovedByTeacherUserId { get; set; } = string.Empty;
    public DateTimeOffset ApprovedAt { get; set; }
    public int TimeKey { get; set; }
    public int MicroSkillCount { get; set; }
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public int MasteredMicroSkillCount { get; set; }
    public int TotalMicroSkillCount { get; set; }
    public DateTimeOffset IngestedAt { get; set; }
}

public sealed class EdwInterventionFact : ITenantEntity, IOrganisationTenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid InterventionId { get; set; }
    public Guid OrganisationId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public Guid LearningGapId { get; set; }
    public string AssignedTeacherUserId { get; set; } = string.Empty;
    public string? InterventionType { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int TimeKey { get; set; }
    public DateTimeOffset IngestedAt { get; set; }
}
