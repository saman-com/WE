using AiGatewayService.Infrastructure.Prompts;
using Microsoft.Extensions.Configuration;

namespace AiGatewayService.Tests;

public class PromptRegistryTests
{
    [Fact]
    public async Task FilePromptRegistry_LoadsVersionedTemplateWithMetadata()
    {
        var promptsRoot = ResolvePromptsRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Prompts:RootPath"] = promptsRoot
            })
            .Build();

        var registry = new FilePromptRegistry(configuration);
        var template = await registry.GetTemplateAsync("assessment-feedback", "1.0.0");

        Assert.NotNull(template);
        Assert.Equal("assessment-feedback", template.Metadata.Id);
        Assert.Equal("1.0.0", template.Metadata.Version);
        Assert.Equal("Assessment Feedback", template.Metadata.Category);
        Assert.Contains("studentName", template.Metadata.Variables);
        Assert.Contains("{{studentName}}", template.Body);
    }

    private static string ResolvePromptsRoot() => AiGatewayWebApplicationFactory.ResolvePromptsRoot();
}
