using VcsManager.Builder;

namespace VcsManager.Gitea.Tests;

public sealed class VcsProviderBuilderExtensionsTests
{
    [Fact]
    public void WithGitea_RegistersProviderAndBuilds()
    {
        var builder = new VcsProviderBuilder()
            .WithGitea(options =>
            {
                options.BaseUrl = "https://gitea.example.com";
                options.Token = "test-token";
                options.DefaultOwner = "test-owner";
            });

        var provider = builder.Build();

        Assert.IsType<GiteaProvider>(provider);
    }

    [Fact]
    public void WithGitea_WithoutToken_ThrowsArgumentException()
    {
        var builder = new VcsProviderBuilder()
            .WithGitea(options => options.BaseUrl = "https://gitea.example.com");

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());

        Assert.Contains("token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithGitea_WithoutConfiguration_RequiresProviderSelection()
    {
        var builder = new VcsProviderBuilder().WithGitea();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("No provider configured", exception.Message);
    }

    [Fact]
    public void WithGitea_CanBeCombinedWithEnvironmentConfiguration()
    {
        const string prefix = "VCSM_TEST_GITEA_ENV";
        Environment.SetEnvironmentVariable($"{prefix}_PROVIDER", "gitea");
        Environment.SetEnvironmentVariable($"{prefix}_GITEA_BASE_URL", "https://gitea.example.com");
        Environment.SetEnvironmentVariable($"{prefix}_GITEA_TOKEN", "test-token");

        try
        {
            var builder = new VcsProviderBuilder()
                .WithGitea()
                .FromEnvironment(prefix);

            var provider = builder.Build();

            Assert.IsType<GiteaProvider>(provider);
        }
        finally
        {
            Environment.SetEnvironmentVariable($"{prefix}_PROVIDER", null);
            Environment.SetEnvironmentVariable($"{prefix}_GITEA_BASE_URL", null);
            Environment.SetEnvironmentVariable($"{prefix}_GITEA_TOKEN", null);
        }
    }
}
