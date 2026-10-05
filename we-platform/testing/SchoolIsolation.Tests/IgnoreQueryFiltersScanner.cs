using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SchoolIsolation.Tests;

internal static class IgnoreQueryFiltersScanner
{
    private static readonly Regex CallPattern = new(
        @"IgnoreQueryFilters\s*\(",
        RegexOptions.Compiled);

    private static readonly Regex MethodPattern = new(
        @"^\s*(?:public|private|internal|protected)\s+(?:(?:static|async|virtual|override|sealed|partial|new|extern)\s+)*[\w.<>,\[\]?]+\s+(\w+)\s*\(",
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
                results.AddRange(ScanFile(relative, File.ReadAllLines(file)));
            }
        }

        return results;
    }

    public static IReadOnlyList<IgnoreQueryFiltersSite> ScanFile(string relativePath, string[] lines)
    {
        var results = new List<IgnoreQueryFiltersSite>();
        var occurrenceByMethod = new Dictionary<string, int>(StringComparer.Ordinal);
        var currentMethod = "<top-level>";
        var braceDepth = 0;
        var methodDepth = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            // Detect method / local-function headers before counting braces on this line.
            var methodMatch = MethodPattern.Match(line);
            if (methodMatch.Success
                && !trimmed.Contains(';', StringComparison.Ordinal)
                && !trimmed.Contains("=>", StringComparison.Ordinal)
                && !trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                currentMethod = methodMatch.Groups[1].Value;
                methodDepth = braceDepth;
            }

            if (CallPattern.IsMatch(line))
            {
                var marker = ExtractTenantBypassMarker(line);
                var keyMethod = marker ?? currentMethod;
                occurrenceByMethod.TryGetValue(keyMethod, out var count);
                count++;
                occurrenceByMethod[keyMethod] = count;
                results.Add(new IgnoreQueryFiltersSite(
                    relativePath,
                    keyMethod,
                    count,
                    i + 1,
                    trimmed));
            }

            foreach (var ch in line)
            {
                if (ch == '{')
                {
                    braceDepth++;
                }
                else if (ch == '}')
                {
                    braceDepth--;
                    if (braceDepth <= methodDepth)
                    {
                        currentMethod = "<top-level>";
                        methodDepth = 0;
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Optional stable marker: // tenant-bypass: MyStableId
    /// When present, used instead of the enclosing method name.
    /// </summary>
    public static string? ExtractTenantBypassMarker(string line)
    {
        const string prefix = "tenant-bypass:";
        var index = line.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var rest = line[(index + prefix.Length)..].Trim();
        if (rest.Length == 0)
        {
            return null;
        }

        var end = rest.IndexOfAny([' ', '\t', '/', ',']);
        return end < 0 ? rest : rest[..end];
    }

    public static string SiteKey(IgnoreQueryFiltersSite site) =>
        $"{site.File}::{site.Method}#{site.Occurrence}";

    public static string EntryKey(IgnoreQueryFiltersAllowlistEntry entry) =>
        $"{entry.File}::{entry.Method}#{entry.Occurrence}";

    public static IgnoreQueryFiltersAllowlist LoadAllowlist(string path)
    {
        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<IgnoreQueryFiltersAllowlist>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return document ?? throw new InvalidOperationException($"Unable to parse allowlist at {path}.");
    }

    public static string SerializeAllowlist(IgnoreQueryFiltersAllowlist allowlist)
    {
        return JsonSerializer.Serialize(allowlist, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        }) + "\n";
    }
}

internal sealed record IgnoreQueryFiltersSite(
    string File,
    string Method,
    int Occurrence,
    int Line,
    string Snippet);

internal sealed class IgnoreQueryFiltersAllowlist
{
    public string? GeneratedFrom { get; set; }
    public string? KeyFormat { get; set; }
    public List<IgnoreQueryFiltersAllowlistEntry> Entries { get; set; } = [];
}

internal sealed class IgnoreQueryFiltersAllowlistEntry
{
    public string File { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int Occurrence { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? Line { get; set; }
}
