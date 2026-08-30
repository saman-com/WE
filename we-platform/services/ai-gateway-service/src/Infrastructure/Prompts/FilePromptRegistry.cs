using System.Text.Json;
using AiGatewayService.Application;
using AiGatewayService.Domain;
using Microsoft.Extensions.Configuration;

namespace AiGatewayService.Infrastructure.Prompts;

public sealed class FilePromptRegistry(IConfiguration configuration) : IPromptRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<PromptTemplate?> GetTemplateAsync(
        string promptId,
        string version,
        CancellationToken cancellationToken = default)
    {
        var rootPath = configuration["Prompts:RootPath"]
            ?? throw new InvalidOperationException("Prompts:RootPath is not configured.");

        var versionFolder = version.Split('.')[0].TrimStart('0');
        if (string.IsNullOrEmpty(versionFolder))
        {
            versionFolder = "1";
        }

        var templateDirectory = Path.Combine(rootPath, promptId, $"v{versionFolder}");
        if (!Directory.Exists(templateDirectory))
        {
            return null;
        }

        var metadataPath = Path.Combine(templateDirectory, "metadata.json");
        var templatePath = Path.Combine(templateDirectory, "template.md");
        if (!File.Exists(metadataPath) || !File.Exists(templatePath))
        {
            return null;
        }

        await using var metadataStream = File.OpenRead(metadataPath);
        var metadataDto = await JsonSerializer.DeserializeAsync<PromptMetadataDto>(
            metadataStream,
            JsonOptions,
            cancellationToken);

        if (metadataDto is null
            || !string.Equals(metadataDto.Id, promptId, StringComparison.Ordinal)
            || !string.Equals(metadataDto.Version, version, StringComparison.Ordinal))
        {
            return null;
        }

        var body = await File.ReadAllTextAsync(templatePath, cancellationToken);
        var metadata = new PromptTemplateMetadata(
            metadataDto.Id,
            metadataDto.Version,
            metadataDto.Category,
            metadataDto.Description,
            metadataDto.Variables);

        return new PromptTemplate(metadata, body);
    }

    private sealed record PromptMetadataDto(
        string Id,
        string Version,
        string Category,
        string Description,
        IReadOnlyList<string> Variables);
}
