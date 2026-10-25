using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaProviderFactoryTests
{
    [Fact]
    public void ProviderName_IsGitea()
    {
        var factory = new GiteaProviderFactory();

        Assert.Equal("gitea", factory.ProviderName);
    }

    [Fact]
    public void MapConfiguration_WithFullSettings_MapsAllValues()
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["token"] = "test-token",
            ["base.url"] = "https://gitea.example.com",
            ["default.owner"] = "test-owner",
        };

        var options = GiteaProviderFactory.MapConfiguration(configuration);

        Assert.Equal("test-token", options.Token);
        Assert.Equal("https://gitea.example.com", options.BaseUrl);
        Assert.Equal("test-owner", options.DefaultOwner);
    }

    [Fact]
    public void MapConfiguration_WithPartialSettings_UsesDefaults()
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["token"] = "test-token",
            ["base.url"] = "https://gitea.example.com",
        };

        var options = GiteaProviderFactory.MapConfiguration(configuration);

        Assert.Null(options.DefaultOwner);
    }

    [Fact]
    public void MapConfiguration_WithEmptySettings_UsesDefaults()
    {
        var options = GiteaProviderFactory.MapConfiguration(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        Assert.Null(options.Token);
        Assert.Null(options.BaseUrl);
        Assert.Null(options.DefaultOwner);
    }

    [Fact]
    public void MapConfiguration_IgnoresWhitespaceValues()
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["token"] = "  ",
            ["base.url"] = "  ",
            ["default.owner"] = "  ",
        };

        var options = GiteaProviderFactory.MapConfiguration(configuration);

        Assert.Null(options.Token);
        Assert.Null(options.BaseUrl);
        Assert.Null(options.DefaultOwner);
    }

    [Fact]
    public void Create_WithoutBaseUrl_ThrowsClearArgumentException()
    {
        var factory = new GiteaProviderFactory();

        var exception = Assert.Throws<ArgumentException>(() => factory.Create(
            new Dictionary<string, string> { ["token"] = "test-token" }));

        Assert.Contains("base url", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithoutToken_ThrowsClearArgumentException()
    {
        var factory = new GiteaProviderFactory();

        var exception = Assert.Throws<ArgumentException>(() => factory.Create(
            new Dictionary<string, string> { ["base.url"] = "https://gitea.example.com" }));

        Assert.Contains("token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithSettings_ReturnsGiteaProvider()
    {
        var factory = new GiteaProviderFactory();

        var provider = factory.Create(CreateConfiguration());

        Assert.IsType<GiteaProvider>(provider);
    }

    [Fact]
    public void ApiClient_UsesNormalizedBaseAddress()
    {
        var client = new GiteaApiClient(new GiteaOptions
        {
            BaseUrl = "https://gitea.example.com",
            Token = "test-token",
        });

        Assert.Equal(new Uri("https://gitea.example.com/api/v1/"), client.BaseAddress);
    }

    internal static Dictionary<string, string> CreateConfiguration()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["base.url"] = "https://gitea.example.com",
            ["token"] = "test-token",
            ["default.owner"] = "test-owner",
        };
    }
}
