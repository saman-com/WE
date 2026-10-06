using Microsoft.Extensions.Configuration;
using OrganisationService.Api;

namespace OrganisationService.Tests;

public class DownstreamServiceUrlTests
{
    [Fact]
    public void StartupCheck_FailsWhenEiBaseUrlIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Intervention:BaseUrl"] = "http://intervention-service:8080"
            })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(
            () => DownstreamServiceUrls.RequireConfigured(configuration));
        Assert.Contains("Ei:BaseUrl", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StartupCheck_FailsWhenInterventionBaseUrlIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ei:BaseUrl"] = "http://ei-service:8080"
            })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(
            () => DownstreamServiceUrls.RequireConfigured(configuration));
        Assert.Contains("Intervention:BaseUrl", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ComposeAndCi_SetBothBaseUrlsForOrganisationService()
    {
        var root = FindRepoRoot();
        var compose = File.ReadAllText(Path.Combine(root, "we-platform", "docker-compose.yml"));
        var organisationBlock = OrganisationServiceBlock(compose);

        Assert.Contains("Ei__BaseUrl: ${Ei__BaseUrl:-http://ei-service:8080}", organisationBlock, StringComparison.Ordinal);
        Assert.Contains(
            "Intervention__BaseUrl: ${Intervention__BaseUrl:-http://intervention-service:8080}",
            organisationBlock,
            StringComparison.Ordinal);

        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        Assert.Contains("Ei__BaseUrl: http://ei-service:8080", workflow, StringComparison.Ordinal);
        Assert.Contains("Intervention__BaseUrl: http://intervention-service:8080", workflow, StringComparison.Ordinal);
    }

    private static string OrganisationServiceBlock(string compose)
    {
        const string header = "  organisation-service:\n";
        var start = compose.IndexOf(header, StringComparison.Ordinal);
        Assert.True(start >= 0, "organisation-service block is missing from docker-compose.yml");
        var next = compose.IndexOf("\n  curriculum-service:\n", start, StringComparison.Ordinal);
        Assert.True(next > start, "organisation-service block has no end");
        return compose[start..next];
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "we-platform", "docker-compose.yml"))
                && File.Exists(Path.Combine(dir.FullName, ".github", "workflows", "ci.yml")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }
}
