using System.Text.Json;
using System.Text.RegularExpressions;

namespace SchoolIsolation.Tests;

internal static class EndpointSourceScanner
{
    private static readonly string[] IdNames =
    [
        "organisationId", "organizationId", "classId", "studentUserId", "assessmentId", "evidenceId",
        "interventionId", "reportId", "curriculumId", "parentCurriculumId", "submissionId", "yearLevelId",
        "parentUserId", "notificationId", "auditLogId", "schoolTenantId", "teacherUserId", "unitId",
        "subjectId", "topicId", "learningObjectiveId", "microSkillId", "userId", "federationId"
    ];

    private static readonly Regex IdPattern = new(
        @"\{(" + string.Join("|", IdNames) + @")(?::[^}]*)?\}",
        RegexOptions.Compiled);

    private static readonly Regex MapPattern = new(
        @"(\w+)\.Map(Get|Post|Put|Patch|Delete)\(\s*""([^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex MapMultilinePattern = new(
        @"(\w+)\.Map(Get|Post|Put|Patch|Delete)\(\s*\n\s*""([^""]+)""",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex AppGroupPattern = new(
        @"var\s+(\w+)\s*=\s*app\.MapGroup\(\s*""([^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex NestedGroupPattern = new(
        @"var\s+(\w+)\s*=\s*(\w+)\.MapGroup\(\s*""([^""]+)""",
        RegexOptions.Compiled);

    public static IReadOnlyList<DiscoveredEndpoint> ScanServicesApi(string servicesRoot)
    {
        var results = new List<DiscoveredEndpoint>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var apiDir in Directory.GetDirectories(servicesRoot)
                     .Select(serviceDir => Path.Combine(serviceDir, "src", "Api"))
                     .Where(Directory.Exists)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var service = new DirectoryInfo(Path.Combine(apiDir, "..", "..")).Name;
            foreach (var file in Directory.EnumerateFiles(apiDir, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                var groups = ResolveGroups(text);
                foreach (var regex in new[] { MapPattern, MapMultilinePattern })
                {
                    foreach (Match match in regex.Matches(text))
                    {
                        var variable = match.Groups[1].Value;
                        var method = match.Groups[2].Value.ToUpperInvariant();
                        var path = match.Groups[3].Value;
                        if (!TryResolveRoute(variable, path, groups, out var route))
                        {
                            continue;
                        }

                        var key = $"{service}|{method}|{route}";
                        if (!seen.Add(key))
                        {
                            continue;
                        }

                        var ids = IdPattern.Matches(route)
                            .Select(m => m.Groups[1].Value)
                            .Distinct(StringComparer.Ordinal)
                            .ToList();

                        results.Add(new DiscoveredEndpoint(service, method, route, ids, file));
                    }
                }
            }
        }

        return results
            .OrderBy(e => e.Service, StringComparer.Ordinal)
            .ThenBy(e => e.Route, StringComparer.Ordinal)
            .ThenBy(e => e.Method, StringComparer.Ordinal)
            .ToList();
    }

    public static IsolationCatalog LoadCatalog(string catalogPath)
    {
        var json = File.ReadAllText(catalogPath);
        var document = JsonSerializer.Deserialize<IsolationCatalog>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return document ?? throw new InvalidOperationException($"Unable to parse catalog at {catalogPath}.");
    }

    private static Dictionary<string, string> ResolveGroups(string text)
    {
        var groups = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in AppGroupPattern.Matches(text))
        {
            groups[match.Groups[1].Value] = match.Groups[2].Value;
        }

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (Match match in NestedGroupPattern.Matches(text))
            {
                var name = match.Groups[1].Value;
                if (groups.ContainsKey(name))
                {
                    continue;
                }

                var parent = match.Groups[2].Value;
                var segment = match.Groups[3].Value;
                if (parent == "app")
                {
                    groups[name] = segment;
                    changed = true;
                }
                else if (groups.TryGetValue(parent, out var parentRoute))
                {
                    groups[name] = $"{parentRoute.TrimEnd('/')}/{segment.TrimStart('/')}";
                    changed = true;
                }
            }
        }

        return groups;
    }

    private static bool TryResolveRoute(
        string variable,
        string path,
        IReadOnlyDictionary<string, string> groups,
        out string route)
    {
        if (variable == "app")
        {
            route = Normalize(path);
            return true;
        }

        if (!groups.TryGetValue(variable, out var prefix))
        {
            route = string.Empty;
            return false;
        }

        route = path is "" or "/"
            ? Normalize(prefix)
            : Normalize($"{prefix.TrimEnd('/')}/{path.TrimStart('/')}");
        return true;
    }

    private static string Normalize(string route) =>
        "/" + Regex.Replace(route, "/+", "/").Trim('/');
}

internal sealed record DiscoveredEndpoint(
    string Service,
    string Method,
    string Route,
    IReadOnlyList<string> Ids,
    string FilePath);

internal sealed class IsolationCatalog
{
    public string? GeneratedFrom { get; set; }
    public List<CatalogEntry> Entries { get; set; } = [];
}

internal sealed class CatalogEntry
{
    public string Service { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public List<string> Ids { get; set; } = [];
    public string Kind { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? File { get; set; }
    public string? Probe { get; set; }
}
