namespace VcsManager.Tests;

public class PagedResultTests
{
    [Fact]
    public void ReportsSuppliedPageMetadata()
    {
        var items = new[] { "alpha", "beta" };

        var result = new PagedResult<string>
        {
            Items = items,
            Page = 2,
            PageSize = 2,
            TotalCount = 5,
            HasNextPage = true,
        };

        Assert.Same(items, result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void Defaults_DescribeAnEmptyFirstPage()
    {
        var result = new PagedResult<int>();

        Assert.Empty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(30, result.PageSize);
        Assert.Null(result.TotalCount);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void TotalCount_IsNullWhenProviderDoesNotReportIt()
    {
        var result = new PagedResult<string>
        {
            Items = ["alpha"],
            Page = 1,
            PageSize = 1,
            HasNextPage = false,
        };

        Assert.Null(result.TotalCount);
    }
}
