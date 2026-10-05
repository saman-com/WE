namespace WePlatform.AspNetCore;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    bool HasMore,
    string? NextCursor);

public static class Paging
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    public static int ClampPageSize(int? pageSize) =>
        pageSize is null or <= 0 ? DefaultPageSize : Math.Min(pageSize.Value, MaxPageSize);

    /// <summary>
    /// Resolves a 1-based page number from an explicit page or a numeric cursor.
    /// </summary>
    public static int ResolvePage(string? cursor, int? page)
    {
        if (page is > 0)
        {
            return page.Value;
        }

        if (!string.IsNullOrWhiteSpace(cursor)
            && int.TryParse(cursor, out var fromCursor)
            && fromCursor > 0)
        {
            return fromCursor;
        }

        return 1;
    }

    public static PagedResponse<T> ToPage<T>(IReadOnlyList<T> window, int page, int pageSize)
    {
        var hasMore = window.Count > pageSize;
        var items = hasMore ? window.Take(pageSize).ToList() : window;
        return new PagedResponse<T>(items, hasMore, hasMore ? (page + 1).ToString() : null);
    }
}
