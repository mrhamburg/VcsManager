using VcsManager.Builder;

namespace VcsManager.GitLab.Tests;

public sealed class VcsProviderBuilderExtensionsTests
{
    [Fact]
    public void WithGitLab_RegistersProviderAndBuilds()
    {
        var builder = new VcsProviderBuilder()
            .WithGitLab(options =>
            {
                options.Token = "test-token";
                options.DefaultNamespace = "test-group";
            });

        var provider = builder.Build();

        Assert.IsType<GitLabProvider>(provider);
    }

    [Fact]
    public void WithGitLab_WithoutToken_ThrowsArgumentException()
    {
        var builder = new VcsProviderBuilder()
            .WithGitLab(options => options.DefaultNamespace = "test-group");

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());

        Assert.Contains("token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithGitLab_WithoutConfiguration_RequiresProviderSelection()
    {
        var builder = new VcsProviderBuilder().WithGitLab();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("No provider configured", exception.Message);
    }

    [Fact]
    public void WithGitLab_WithSelfHostedBaseUrl_ConfiguresUrl()
    {
        var builder = new VcsProviderBuilder()
            .WithGitLab(options =>
            {
                options.Token = "test-token";
                options.BaseUrl = "https://gitlab.example.com";
            });

        var provider = builder.Build();

        Assert.IsType<GitLabProvider>(provider);
    }

    [Fact]
    public void WithGitLab_CanBeCombinedWithEnvironmentConfiguration()
    {
        const string prefix = "VCSM_TEST_GITLAB_ENV";
        Environment.SetEnvironmentVariable($"{prefix}_PROVIDER", "gitlab");
        Environment.SetEnvironmentVariable($"{prefix}_GITLAB_TOKEN", "test-token");

        try
        {
            var builder = new VcsProviderBuilder()
                .WithGitLab()
                .FromEnvironment(prefix);

            var provider = builder.Build();

            Assert.IsType<GitLabProvider>(provider);
        }
        finally
        {
            Environment.SetEnvironmentVariable($"{prefix}_PROVIDER", null);
            Environment.SetEnvironmentVariable($"{prefix}_GITLAB_TOKEN", null);
        }
    }
}
