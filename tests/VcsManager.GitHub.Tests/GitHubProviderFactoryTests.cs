using VcsManager.Builder;
using VcsManager.GitHub.Internal;

namespace VcsManager.GitHub.Tests;

public sealed class GitHubProviderFactoryTests
{
    [Fact]
    public void ProviderName_IsGitHub()
    {
        var factory = new GitHubProviderFactory();

        Assert.Equal("github", factory.ProviderName);
    }

    [Fact]
    public void MapConfiguration_WithFullSettings_MapsAllValues()
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["token"] = "test-token",
            ["base.url"] = "https://github.example.com/api/v3",
            ["default.owner"] = "test-owner",
        };

        var options = GitHubProviderFactory.MapConfiguration(configuration);

        Assert.Equal("test-token", options.Token);
        Assert.Equal("https://github.example.com/api/v3", options.BaseUrl);
        Assert.Equal("test-owner", options.DefaultOwner);
    }

    [Fact]
    public void MapConfiguration_WithPartialSettings_UsesDefaults()
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["token"] = "test-token",
        };

        var options = GitHubProviderFactory.MapConfiguration(configuration);

        Assert.Equal("test-token", options.Token);
        Assert.Equal(GitHubOptions.DefaultBaseUrl, options.BaseUrl);
        Assert.Null(options.DefaultOwner);
    }

    [Fact]
    public void MapConfiguration_WithEmptySettings_UsesDefaults()
    {
        var options = GitHubProviderFactory.MapConfiguration(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        Assert.Null(options.Token);
        Assert.Equal(GitHubOptions.DefaultBaseUrl, options.BaseUrl);
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

        var options = GitHubProviderFactory.MapConfiguration(configuration);

        Assert.Null(options.Token);
        Assert.Equal(GitHubOptions.DefaultBaseUrl, options.BaseUrl);
        Assert.Null(options.DefaultOwner);
    }

    [Fact]
    public void OctokitClient_UsesDefaultBaseAddress()
    {
        var client = new OctokitGitHubApiClient(new GitHubOptions { Token = "test-token" });

        Assert.Equal(new Uri("https://api.github.com/"), client.BaseAddress);
    }

    [Fact]
    public void OctokitClient_UsesEnterpriseBaseAddress()
    {
        var client = new OctokitGitHubApiClient(new GitHubOptions
        {
            Token = "test-token",
            BaseUrl = "https://github.example.com/api/v3",
        });

        Assert.Equal(new Uri("https://github.example.com/api/v3/"), client.BaseAddress);
    }

    [Fact]
    public void Create_WithoutToken_ThrowsClearArgumentException()
    {
        var factory = new GitHubProviderFactory();

        var exception = Assert.Throws<ArgumentException>(
            () => factory.Create(new Dictionary<string, string>()));

        Assert.Contains("token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithToken_ReturnsGitHubProvider()
    {
        var factory = new GitHubProviderFactory();

        var provider = factory.Create(new Dictionary<string, string>
        {
            ["token"] = "test-token",
        });

        Assert.IsType<GitHubProvider>(provider);
    }
}
