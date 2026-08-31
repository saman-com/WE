namespace FederationService.Domain;

public sealed class SchoolAdminAssignment
{
    public Guid Id { get; set; }
    public Guid FederationId { get; set; }
    public Guid SchoolTenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset AssignedAt { get; set; }

    public FederationSchool? School { get; set; }
}
