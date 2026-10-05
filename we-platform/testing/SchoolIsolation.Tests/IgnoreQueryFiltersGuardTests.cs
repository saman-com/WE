namespace SchoolIsolation.Tests;

public class IgnoreQueryFiltersGuardTests
{
    [Fact]
    public void Every_IgnoreQueryFilters_In_Services_Src_Is_Allowlisted_With_Reason()
    {
        var servicesRoot = FindServicesRoot();
        var allowlistPath = Path.Combine(AppContext.BaseDirectory, "ignore-query-filters-allowlist.json");
        Assert.True(File.Exists(allowlistPath), $"Missing allowlist at {allowlistPath}");

        var discovered = IgnoreQueryFiltersScanner.ScanServicesSrc(servicesRoot);
        var allowlist = IgnoreQueryFiltersScanner.LoadAllowlist(allowlistPath);

        Assert.NotEmpty(discovered);
        Assert.NotEmpty(allowlist.Entries);

        foreach (var entry in allowlist.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Reason),
                $"Allowlist entry {entry.File}:{entry.Line} must declare a reason.");
        }

        var allowlistKeys = allowlist.Entries
            .Select(e => Key(e.File, e.Line))
            .ToHashSet(StringComparer.Ordinal);

        var missing = discovered
            .Select(site => Key(site.File, site.Line))
            .Where(key => !allowlistKeys.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        var stale = allowlist.Entries
            .Select(e => Key(e.File, e.Line))
            .Where(key => discovered.All(d => Key(d.File, d.Line) != key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "IgnoreQueryFilters() sites in services/*/src missing from allowlist "
            + "(add an entry with a reason, or remove the bypass):\n"
            + string.Join("\n", missing));

        Assert.True(
            stale.Count == 0,
            "Allowlist entries that no longer exist in source (remove or update lines):\n"
            + string.Join("\n", stale));
    }

    private static string Key(string file, int line) => $"{file}:{line}";

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
