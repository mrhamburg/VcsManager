namespace VcsManager.Gitea.Tests;

public sealed class GiteaOptionsTests
{
    [Theory]
    [InlineData("https://gitea.example.com")]
    [InlineData("https://gitea.example.com/")]
    [InlineData("https://gitea.example.com/api/v1")]
    [InlineData("https://gitea.example.com/api/v1/")]
    public void CreateApiBaseAddress_NormalizesBaseUrl(string baseUrl)
    {
        var options = new GiteaOptions { BaseUrl = baseUrl, Token = "test-token" };

        Assert.Equal(new Uri("https://gitea.example.com/api/v1/"), options.CreateApiBaseAddress());
    }

    [Fact]
    public void Validate_WithNonHttpUrl_ThrowsArgumentException()
    {
        var options = new GiteaOptions
        {
            BaseUrl = "ftp://gitea.example.com",
            Token = "test-token",
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithoutToken_ThrowsArgumentException()
    {
        var options = new GiteaOptions { BaseUrl = "https://gitea.example.com" };

        var exception = Assert.Throws<ArgumentException>(() => options.Validate());

        Assert.Contains("token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_DoesNotIncludeTokenInMessage()
    {
        var options = new GiteaOptions { Token = "super-secret-token" };

        var exception = Assert.Throws<ArgumentException>(() => options.Validate());

        Assert.DoesNotContain("super-secret-token", exception.Message, StringComparison.Ordinal);
    }
}
