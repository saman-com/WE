namespace AssessmentService.Domain;

using WePlatform.Tenancy;

public sealed class AiFeedbackAuditLog : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid MicroSkillId { get; set; }
    public string TeacherUserId { get; set; } = string.Empty;
    public string PromptId { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string PromptVariablesJson { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
    public string? TeacherEditedFeedback { get; set; }
    public Guid? EvidenceId { get; set; }
    public DateTimeOffset? FinalApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
