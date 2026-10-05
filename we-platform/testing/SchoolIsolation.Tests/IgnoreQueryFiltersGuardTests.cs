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
                $"Allowlist entry {IgnoreQueryFiltersScanner.EntryKey(entry)} must declare a reason.");
            Assert.False(string.IsNullOrWhiteSpace(entry.Method),
                $"Allowlist entry {entry.File} must declare a method (or tenant-bypass marker).");
            Assert.True(entry.Occurrence >= 1,
                $"Allowlist entry {IgnoreQueryFiltersScanner.EntryKey(entry)} must have occurrence >= 1.");
        }

        var allowlistKeys = allowlist.Entries
            .Select(IgnoreQueryFiltersScanner.EntryKey)
            .ToHashSet(StringComparer.Ordinal);

        var missing = discovered
            .Select(IgnoreQueryFiltersScanner.SiteKey)
            .Where(key => !allowlistKeys.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        var discoveredKeys = discovered
            .Select(IgnoreQueryFiltersScanner.SiteKey)
            .ToHashSet(StringComparer.Ordinal);

        var stale = allowlist.Entries
            .Select(IgnoreQueryFiltersScanner.EntryKey)
            .Where(key => !discoveredKeys.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "IgnoreQueryFilters() sites in services/*/src missing from allowlist "
            + "(add an entry with file/method/occurrence and a reason, or remove the bypass):\n"
            + string.Join("\n", missing));

        Assert.True(
            stale.Count == 0,
            "Allowlist entries that no longer exist in source (remove or update method/occurrence):\n"
            + string.Join("\n", stale));
    }

    [Fact]
    public void Site_Key_Is_Stable_When_Lines_Shift_Within_The_Same_Method()
    {
        var before = """
            public class Sample
            {
                public void FinalizeAudit()
                {
                    var x = db.Items.IgnoreQueryFilters().First();
                }
            }
            """.ReplaceLineEndings("\n").Split('\n');

        var after = """
            public class Sample
            {
                public void FinalizeAudit()
                {
                    // blank line inserted above the bypass
                    var x = db.Items.IgnoreQueryFilters().First();
                }
            }
            """.ReplaceLineEndings("\n").Split('\n');

        var beforeSites = IgnoreQueryFiltersScanner.ScanFile("services/demo/Sample.cs", before);
        var afterSites = IgnoreQueryFiltersScanner.ScanFile("services/demo/Sample.cs", after);

        Assert.Single(beforeSites);
        Assert.Single(afterSites);
        Assert.Equal(
            IgnoreQueryFiltersScanner.SiteKey(beforeSites[0]),
            IgnoreQueryFiltersScanner.SiteKey(afterSites[0]));
        Assert.Equal("FinalizeAudit", beforeSites[0].Method);
        Assert.Equal(1, beforeSites[0].Occurrence);
        Assert.NotEqual(beforeSites[0].Line, afterSites[0].Line);
    }

    [Fact]
    public void New_Bypass_In_Unlisted_Method_Is_Detected_As_Missing()
    {
        var lines = """
            public class Sample
            {
                public void Allowed()
                {
                    _ = db.Items.IgnoreQueryFilters();
                }

                public void NewBypass()
                {
                    _ = db.Items.IgnoreQueryFilters();
                }
            }
            """.ReplaceLineEndings("\n").Split('\n');

        var sites = IgnoreQueryFiltersScanner.ScanFile("services/demo/Sample.cs", lines);
        Assert.Equal(2, sites.Count);

        var allowlistKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "services/demo/Sample.cs::Allowed#1"
        };

        var missing = sites
            .Select(IgnoreQueryFiltersScanner.SiteKey)
            .Where(key => !allowlistKeys.Contains(key))
            .ToList();

        Assert.Contains("services/demo/Sample.cs::NewBypass#1", missing);
    }

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
