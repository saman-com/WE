namespace OrganisationService.Domain;

using WePlatform.Tenancy;

public sealed class ClassTeacher : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ClassId { get; set; }
    public string TeacherUserId { get; set; } = string.Empty;

    public SchoolClass Class { get; set; } = null!;
}
