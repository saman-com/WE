namespace AiGatewayService.Domain;

public sealed record PromptTemplateMetadata(
    string Id,
    string Version,
    string Category,
    string Description,
    IReadOnlyList<string> Variables);

public sealed record PromptTemplate(
    PromptTemplateMetadata Metadata,
    string Body);
