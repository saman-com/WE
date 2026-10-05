using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using NationalReportingService.Application;

namespace NationalReportingService.Api;

/// <summary>
/// Adds ministry-facing documentation on the generated CountCell schema.
/// </summary>
internal sealed class CountCellSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(CountCell))
        {
            return Task.CompletedTask;
        }

        schema.Description =
            "Aggregate count cell with small-count suppression. When suppressed is true, "
            + "value is null; consumers must not treat the cell as 0 (SP-001 Ch.22.10).";

        if (schema.Properties is not null)
        {
            if (schema.Properties.TryGetValue("value", out var valueSchema)
                || schema.Properties.TryGetValue("Value", out valueSchema))
            {
                valueSchema.Description = "Visible count, or null when suppressed.";
            }

            if (schema.Properties.TryGetValue("suppressed", out var suppressedSchema)
                || schema.Properties.TryGetValue("Suppressed", out suppressedSchema))
            {
                suppressedSchema.Description =
                    "True when the count is withheld because the group is smaller than "
                    + "NationalReporting:MinimumGroupSize (default 5).";
            }
        }

        return Task.CompletedTask;
    }
}
