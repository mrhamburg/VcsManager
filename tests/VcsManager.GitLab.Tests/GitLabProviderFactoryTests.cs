using VcsManager.GitLab.Internal;

namespace VcsManager.GitLab.Tests;

public sealed class GitLabProviderFactoryTests
{
    [Fact]
    public void ProviderName_IsGitLab()
    {
        var factory = new GitLabProviderFactory();

        Assert.Equal("gitlab", factory.ProviderName);
    }

    [Fact]
    public void MapConfiguration_WithFullSettings_MapsAllValues()
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["token"] = "test-token",
            ["base.url"] = "https://gitlab.example.com",
            ["default.namespace"] = "test-group",
        };

        var options = GitLabProviderFactory.MapConfiguration(configuration);

        Assert.Equal("test-token", options.Token);
        Assert.Equal("https://gitlab.example.com", options.BaseUrl);
        Assert.Equal("test-group", options.DefaultNamespace);
    }

    [Fact]
    public void MapConfiguration_WithPartialSettings_UsesDefaults()
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["token"] = "test-token",
        };

        var options = GitLabProviderFactory.MapConfiguration(configuration);

        Assert.Equal("test-token", options.Token);
        Assert.Equal(GitLabOptions.DefaultBaseUrl, options.BaseUrl);
        Assert.Null(options.DefaultNamespace);
    }

    [Fact]
    public void MapConfiguration_WithEmptySettings_UsesDefaults()
    {
        var options = GitLabProviderFactory.MapConfiguration(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        Assert.Null(options.Token);
        Assert.Equal(GitLabOptions.DefaultBaseUrl, options.BaseUrl);
        Assert.Null(options.DefaultNamespace);
    }

    [Fact]
    public void MapConfiguration_IgnoresWhitespaceValues()
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["token"] = "  ",
            ["base.url"] = "  ",
            ["default.namespace"] = "  ",
        };

        var options = GitLabProviderFactory.MapConfiguration(configuration);

        Assert.Null(options.Token);
        Assert.Equal(GitLabOptions.DefaultBaseUrl, options.BaseUrl);
        Assert.Null(options.DefaultNamespace);
    }

    [Fact]
    public void Create_WithoutToken_ThrowsClearArgumentException()
    {
        var factory = new GitLabProviderFactory();

        var exception = Assert.Throws<ArgumentException>(
            () => factory.Create(new Dictionary<string, string>()));

        Assert.Contains("token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithToken_ReturnsGitLabProvider()
    {
        var factory = new GitLabProviderFactory();

        var provider = factory.Create(new Dictionary<string, string>
        {
            ["token"] = "test-token",
        });

        Assert.IsType<GitLabProvider>(provider);
    }

    [Fact]
    public void ApiClient_UsesDefaultBaseAddress()
    {
        var client = new GitLabApiClient(new GitLabOptions { Token = "test-token" });

        Assert.Equal(new Uri("https://gitlab.com/api/v4/"), client.BaseAddress);
    }

    [Fact]
    public void ApiClient_UsesSelfHostedBaseAddress()
    {
        var client = new GitLabApiClient(new GitLabOptions
        {
            Token = "test-token",
            BaseUrl = "https://gitlab.example.com",
        });

        Assert.Equal(new Uri("https://gitlab.example.com/api/v4/"), client.BaseAddress);
    }

    [Fact]
    public void ApiClient_DoesNotDuplicateApiPath()
    {
        var client = new GitLabApiClient(new GitLabOptions
        {
            Token = "test-token",
            BaseUrl = "https://gitlab.example.com/api/v4",
        });

        Assert.Equal(new Uri("https://gitlab.example.com/api/v4/"), client.BaseAddress);
    }
}
