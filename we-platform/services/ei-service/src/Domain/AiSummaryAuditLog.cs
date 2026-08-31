namespace EiService.Domain;

using WePlatform.Tenancy;

public sealed class AiSummaryAuditLog : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string SummaryType { get; set; } = string.Empty;
    public Guid? OrganisationId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? UnitId { get; set; }
    public string? StudentUserId { get; set; }
    public string TeacherUserId { get; set; } = string.Empty;
    public string PromptId { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string PromptVariablesJson { get; set; } = string.Empty;
    public string ContextScopeJson { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
    public string? TeacherEditedContent { get; set; }
    public DateTimeOffset? FinalApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
