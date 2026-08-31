namespace FederationService.Domain;

public sealed class FederationSchool
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FederationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int EnrollmentCount { get; set; }
    public decimal AverageProgressPercent { get; set; }
    public bool HasDefaultConfiguration { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<SchoolAdminAssignment> AdminAssignments { get; set; } = new List<SchoolAdminAssignment>();
}
