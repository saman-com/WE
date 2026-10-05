using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace OrganisationService.Infrastructure;

/// <summary>
/// Follows cursor-paged JSON responses until <c>hasMore</c> is false.
/// </summary>
internal static class PagedHttp
{
    public const int PageSize = 100;
    public const int MaxPages = 50;

    public sealed record Page<T>(IReadOnlyList<T> Items, bool HasMore, string? NextCursor);

    public static async Task<IReadOnlyList<T>> FetchAllAsync<T>(
        Func<string? /*cursor*/, CancellationToken, Task<Page<T>?>> fetchPage,
        ILogger logger,
        string resourceName,
        CancellationToken cancellationToken)
    {
        var all = new List<T>();
        string? cursor = null;

        for (var pageIndex = 0; pageIndex < MaxPages; pageIndex++)
        {
            var page = await fetchPage(cursor, cancellationToken);
            if (page is null)
            {
                break;
            }

            all.AddRange(page.Items);
            if (!page.HasMore || string.IsNullOrWhiteSpace(page.NextCursor))
            {
                return all;
            }

            cursor = page.NextCursor;
        }

        logger.LogWarning(
            "Stopped paging {Resource} after {MaxPages} pages ({Count} items collected).",
            resourceName,
            MaxPages,
            all.Count);
        return all;
    }

    public static async Task<Page<T>?> ReadPageAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<Page<T>>(cancellationToken: cancellationToken);
}
