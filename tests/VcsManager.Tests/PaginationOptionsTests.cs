namespace VcsManager.Tests;

public class PaginationOptionsTests
{
    [Fact]
    public void Defaults_UseDocumentedValues()
    {
        var options = new PaginationOptions();

        Assert.Equal(1, options.Page);
        Assert.Equal(30, options.PageSize);
    }

    [Fact]
    public void CustomValues_ArePreserved()
    {
        var options = new PaginationOptions { Page = 4, PageSize = 100 };

        Assert.Equal(4, options.Page);
        Assert.Equal(100, options.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_BelowOne_Throws(int page)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new PaginationOptions { Page = page });

        Assert.Equal("Page", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void PageSize_OutOfRange_Throws(int pageSize)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new PaginationOptions { PageSize = pageSize });

        Assert.Equal("PageSize", exception.ParamName);
    }

    [Fact]
    public void WithExpression_ValidatesNewValues()
    {
        var options = new PaginationOptions { Page = 2 };

        Assert.Throws<ArgumentOutOfRangeException>(() => options with { Page = 0 });
    }
}
