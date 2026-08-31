namespace OrganisationService.Domain;

using WePlatform.Tenancy;

public sealed class SchoolClass : ITenantEntity, IOrganisationTenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganisationId { get; set; }
    public Guid YearLevelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public Organisation Organisation { get; set; } = null!;
    public YearLevel YearLevel { get; set; } = null!;
    public ICollection<ClassTeacher> Teachers { get; set; } = new List<ClassTeacher>();
    public ICollection<ClassEnrollment> Enrollments { get; set; } = new List<ClassEnrollment>();
}
