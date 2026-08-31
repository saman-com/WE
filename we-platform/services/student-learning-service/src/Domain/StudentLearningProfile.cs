namespace StudentLearningService.Domain;

using WePlatform.Tenancy;

public sealed class StudentLearningProfile : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<ProfileClassEnrollment> Enrollments { get; set; } = [];
    public List<ProfileEvidenceEntry> EvidenceEntries { get; set; } = [];
}
