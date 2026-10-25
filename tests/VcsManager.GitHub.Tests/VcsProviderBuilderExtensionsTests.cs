using VcsManager.Builder;

namespace VcsManager.GitHub.Tests;

public sealed class VcsProviderBuilderExtensionsTests
{
    [Fact]
    public void WithGitHub_RegistersProviderAndBuilds()
    {
        var builder = new VcsProviderBuilder()
            .WithGitHub(options =>
            {
                options.Token = "test-token";
                options.DefaultOwner = "test-owner";
            });

        var provider = builder.Build();

        Assert.IsType<GitHubProvider>(provider);
    }

    [Fact]
    public void WithGitHub_WithoutToken_ThrowsArgumentException()
    {
        var builder = new VcsProviderBuilder().WithGitHub(options => options.DefaultOwner = "test-owner");

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());

        Assert.Contains("token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithGitHub_WithoutConfiguration_RequiresProviderSelection()
    {
        var builder = new VcsProviderBuilder().WithGitHub();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("No provider configured", exception.Message);
    }

    [Fact]
    public void WithGitHub_WithBaseUrl_ConfiguresEnterpriseUrl()
    {
        var builder = new VcsProviderBuilder()
            .WithGitHub(options =>
            {
                options.Token = "test-token";
                options.BaseUrl = "https://github.example.com/api/v3";
            });

        var provider = builder.Build();

        Assert.IsType<GitHubProvider>(provider);
    }

    [Fact]
    public void WithGitHub_CanBeCombinedWithEnvironmentConfiguration()
    {
        const string prefix = "VCSM_TEST_GITHUB_ENV";
        Environment.SetEnvironmentVariable($"{prefix}_PROVIDER", "github");
        Environment.SetEnvironmentVariable($"{prefix}_GITHUB_TOKEN", "test-token");

        try
        {
            var builder = new VcsProviderBuilder()
                .WithGitHub()
                .FromEnvironment(prefix);

            var provider = builder.Build();

            Assert.IsType<GitHubProvider>(provider);
        }
        finally
        {
            Environment.SetEnvironmentVariable($"{prefix}_PROVIDER", null);
            Environment.SetEnvironmentVariable($"{prefix}_GITHUB_TOKEN", null);
        }
    }
}
