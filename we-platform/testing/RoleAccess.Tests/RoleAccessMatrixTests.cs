using System.Text.Json;

namespace RoleAccess.Tests;

/// <summary>
/// Cross-cutting role×API catalog. Executable probes live in each service's
/// <c>RoleAccessEndpointTests</c> (TestHost + TestJwt). This suite guards the
/// documented allow/deny table stays complete for all PlatformRoles.
/// </summary>
public class RoleAccessMatrixTests
{
    private static readonly string[] PlatformRoles =
    [
        "Student",
        "Teacher",
        "Parent",
        "SchoolLeader",
        "SystemAdministrator",
        "FederationAdmin",
        "EducationAuthorityOfficer"
    ];

    private static readonly string[] RequiredServices =
    [
        "identity-service",
        "organisation-service",
        "assessment-service",
        "federation-service",
        "national-reporting-service",
        "configuration-service"
    ];

    [Fact]
    public void Catalog_CoversAllPlatformRoles_AndRequiredServiceProbes()
    {
        var catalogPath = Path.Combine(AppContext.BaseDirectory, "role-access-catalog.json");
        Assert.True(File.Exists(catalogPath), $"Missing catalog at {catalogPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(catalogPath));
        var root = document.RootElement;

        var catalogRoles = root.GetProperty("roles")
            .EnumerateArray()
            .Select(e => e.GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var role in PlatformRoles)
        {
            Assert.Contains(role, catalogRoles);
        }

        var probes = root.GetProperty("probes").EnumerateArray().ToList();
        Assert.NotEmpty(probes);

        var services = probes
            .Select(p => p.GetProperty("service").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var service in RequiredServices)
        {
            Assert.Contains(service, services);
        }

        foreach (var probe in probes)
        {
            var expected = probe.GetProperty("expected");
            foreach (var role in PlatformRoles)
            {
                Assert.True(
                    expected.TryGetProperty(role, out var status),
                    $"Probe {probe.GetProperty("service").GetString()} {probe.GetProperty("route").GetString()} missing expected status for {role}");
                var code = status.GetInt32();
                Assert.True(
                    code is 200 or 401 or 403,
                    $"Expected status for {role} must be 200/401/403, got {code}");
            }
        }
    }
}
