namespace FederationService.Application;

public sealed record ApiErrorResponse(string Code);

public record CreateFederationSchoolRequest(string Name, string Code);

public record FederationSchoolResponse(
    Guid TenantId,
    Guid FederationId,
    string Name,
    string Code,
    bool HasDefaultConfiguration,
    DateTimeOffset CreatedAt);

public record AssignSchoolAdminRequest(string UserId);

public record SchoolAdminAssignmentResponse(
    Guid SchoolTenantId,
    string UserId,
    DateTimeOffset AssignedAt);

public record SchoolMetricSummary(
    Guid TenantId,
    string Name,
    int EnrollmentCount,
    decimal AverageProgressPercent);

public record FederationMetricsResponse(
    int TotalSchools,
    int TotalEnrollment,
    decimal AverageProgressPercent,
    IReadOnlyList<SchoolMetricSummary> Schools);

public record FederationPolicyDto(string PolicyKey, string PolicyValue);

public record UpdateFederationPoliciesRequest(IReadOnlyList<FederationPolicyDto> Policies);

public interface IRegionalConfigurationProvisioner
{
    Task ProvisionDefaultConfigurationAsync(Guid schoolTenantId, CancellationToken cancellationToken = default);
}
