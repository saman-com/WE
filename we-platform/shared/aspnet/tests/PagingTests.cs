using WePlatform.AspNetCore;

namespace WePlatform.AspNetCore.Tests;

public sealed class PagingTests
{
    [Fact]
    public void ClampPageSize_DefaultsAndCaps()
    {
        Assert.Equal(50, Paging.ClampPageSize(null));
        Assert.Equal(50, Paging.ClampPageSize(0));
        Assert.Equal(50, Paging.ClampPageSize(-1));
        Assert.Equal(25, Paging.ClampPageSize(25));
        Assert.Equal(100, Paging.ClampPageSize(100));
        Assert.Equal(100, Paging.ClampPageSize(250));
    }

    [Fact]
    public void ResolvePage_PrefersExplicitPageThenCursor()
    {
        Assert.Equal(1, Paging.ResolvePage(null, null));
        Assert.Equal(3, Paging.ResolvePage("9", 3));
        Assert.Equal(4, Paging.ResolvePage("4", null));
        Assert.Equal(1, Paging.ResolvePage("nope", null));
    }

    [Fact]
    public void ToPage_SetsHasMoreAndNextCursor()
    {
        var window = Enumerable.Range(1, 51).ToList();
        var page = Paging.ToPage(window, page: 1, pageSize: 50);
        Assert.Equal(50, page.Items.Count);
        Assert.True(page.HasMore);
        Assert.Equal("2", page.NextCursor);

        var last = Paging.ToPage(Enumerable.Range(1, 20).ToList(), page: 3, pageSize: 50);
        Assert.Equal(20, last.Items.Count);
        Assert.False(last.HasMore);
        Assert.Null(last.NextCursor);
    }
}
