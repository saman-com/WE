namespace SchoolIsolation.Tests;

public class EndpointCoverageGuardTests
{
    private static readonly HashSet<string> AllowedKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "isolation",
        "exempt"
    };

    [Fact]
    public void Every_Mapped_Endpoint_Is_In_Isolation_Catalog_Or_Exempt_List()
    {
        var servicesRoot = FindServicesRoot();
        var catalogPath = Path.Combine(AppContext.BaseDirectory, "isolation-catalog.json");
        Assert.True(File.Exists(catalogPath), $"Missing catalog at {catalogPath}");

        var discovered = EndpointSourceScanner.ScanServicesApi(servicesRoot);
        var catalog = EndpointSourceScanner.LoadCatalog(catalogPath);

        Assert.NotEmpty(discovered);
        Assert.NotEmpty(catalog.Entries);

        foreach (var entry in catalog.Entries)
        {
            Assert.Contains(entry.Kind, AllowedKinds);
            if (string.Equals(entry.Kind, "isolation", StringComparison.OrdinalIgnoreCase))
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.Probe),
                    $"Isolation entry {entry.Method} {entry.Route} must declare a probe.");
            }
        }

        var catalogKeys = catalog.Entries
            .Select(e => Key(e.Service, e.Method, e.Route))
            .ToHashSet(StringComparer.Ordinal);

        var missingFromCatalog = discovered
            .Select(e => Key(e.Service, e.Method, e.Route))
            .Where(key => !catalogKeys.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        var extraInCatalog = catalog.Entries
            .Select(e => Key(e.Service, e.Method, e.Route))
            .Where(key => discovered.All(d => Key(d.Service, d.Method, d.Route) != key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missingFromCatalog.Count == 0,
            "Endpoints discovered in source but missing from isolation-catalog.json (add isolation probe or exempt):\n"
            + string.Join("\n", missingFromCatalog));

        Assert.True(
            extraInCatalog.Count == 0,
            "Catalog entries that no longer exist in source (remove or update):\n"
            + string.Join("\n", extraInCatalog));
    }

    [Fact]
    public void Isolation_Probes_Are_Registered()
    {
        var catalogPath = Path.Combine(AppContext.BaseDirectory, "isolation-catalog.json");
        var catalog = EndpointSourceScanner.LoadCatalog(catalogPath);
        var requiredProbes = catalog.Entries
            .Where(e => string.Equals(e.Kind, "isolation", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Probe!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        var registered = ProbeRegistry.All
            .ToHashSet(StringComparer.Ordinal);

        var missing = requiredProbes.Where(p => !registered.Contains(p)).ToList();
        Assert.True(
            missing.Count == 0,
            "isolation-catalog probes without a registered ProbeRegistry entry "
            + "(implement the table-driven isolation test and register the probe id):\n"
            + string.Join("\n", missing));
    }

    private static string Key(string service, string method, string route) =>
        $"{service}|{method}|{route}";

    private static string FindServicesRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "services");
            if (Directory.Exists(candidate) && Directory.Exists(Path.Combine(dir.FullName, "testing")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate we-platform/services from test base directory.");
    }
}
