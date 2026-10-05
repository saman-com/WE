using System.Text.Json;
using System.Text.RegularExpressions;

namespace SchoolIsolation.Tests;

internal static class IgnoreQueryFiltersScanner
{
    private static readonly Regex CallPattern = new(
        @"IgnoreQueryFilters\s*\(",
        RegexOptions.Compiled);

    public static IReadOnlyList<IgnoreQueryFiltersSite> ScanServicesSrc(string servicesRoot)
    {
        var results = new List<IgnoreQueryFiltersSite>();
        foreach (var serviceDir in Directory.GetDirectories(servicesRoot).OrderBy(path => path, StringComparer.Ordinal))
        {
            var srcDir = Path.Combine(serviceDir, "src");
            if (!Directory.Exists(srcDir))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                var relative = Path.GetRelativePath(Path.GetDirectoryName(servicesRoot)!, file)
                    .Replace('\\', '/');
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    if (!CallPattern.IsMatch(lines[i]))
                    {
                        continue;
                    }

                    results.Add(new IgnoreQueryFiltersSite(relative, i + 1, lines[i].Trim()));
                }
            }
        }

        return results;
    }

    public static IgnoreQueryFiltersAllowlist LoadAllowlist(string path)
    {
        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<IgnoreQueryFiltersAllowlist>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return document ?? throw new InvalidOperationException($"Unable to parse allowlist at {path}.");
    }
}

internal sealed record IgnoreQueryFiltersSite(string File, int Line, string Snippet);

internal sealed class IgnoreQueryFiltersAllowlist
{
    public string? GeneratedFrom { get; set; }
    public List<IgnoreQueryFiltersAllowlistEntry> Entries { get; set; } = [];
}

internal sealed class IgnoreQueryFiltersAllowlistEntry
{
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Reason { get; set; } = string.Empty;
}
