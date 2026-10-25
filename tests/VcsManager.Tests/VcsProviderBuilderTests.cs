using VcsManager.Builder;

namespace VcsManager.Tests;

public class VcsProviderBuilderTests
{
    [Fact]
    public void Build_WithoutConfiguration_ThrowsInvalidOperationException()
    {
        var builder = new VcsProviderBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("No provider configured", exception.Message);
    }

    [Fact]
    public void Build_WithUnknownProvider_ListsRegisteredProviders()
    {
        var builder = new VcsProviderBuilder()
            .RegisterProvider(new FakeFactory("GitHub"))
            .RegisterProvider(new FakeFactory("GitLab"))
            .UseProvider("AzureDevOps");

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("AzureDevOps", exception.Message);
        Assert.Contains("GitHub", exception.Message);
        Assert.Contains("GitLab", exception.Message);
    }

    [Fact]
    public void Build_ResolvesRegisteredFactoryAndPassesSettings()
    {
        var factory = new FakeFactory("test");
        var builder = new VcsProviderBuilder()
            .RegisterProvider(factory)
            .UseProvider("test", settings => settings["Token"] = "abc");

        var provider = builder.Build();

        Assert.NotNull(provider);
        Assert.Equal("abc", factory.LastConfiguration?["token"]);
    }

    [Fact]
    public void Build_ProviderNameIsCaseInsensitive()
    {
        var builder = new VcsProviderBuilder()
            .RegisterProvider(new FakeFactory("GitHub"))
            .UseProvider("github");

        var provider = builder.Build();

        Assert.NotNull(provider);
    }

    [Fact]
    public void Configuration_SettingsAreCaseInsensitive()
    {
        var configuration = new VcsProviderConfiguration();
        configuration.Settings["Token"] = "abc";

        Assert.Equal("abc", configuration.Settings["TOKEN"]);
    }

    [Fact]
    public void FromEnvironment_ConfiguresProvider()
    {
        const string prefix = "VCSM_TEST_BUILDER_ENV";
        Environment.SetEnvironmentVariable($"{prefix}_PROVIDER", "test");
        Environment.SetEnvironmentVariable($"{prefix}_TEST_TOKEN", "abc");
        Environment.SetEnvironmentVariable($"{prefix}_TEST_BASE_URL", "https://example.test");

        try
        {
            var factory = new FakeFactory("test");
            var builder = new VcsProviderBuilder()
                .RegisterProvider(factory)
                .FromEnvironment(prefix);

            var provider = builder.Build();

            Assert.NotNull(provider);
            Assert.Equal("abc", factory.LastConfiguration?["token"]);
            Assert.Equal("https://example.test", factory.LastConfiguration?["base.url"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable($"{prefix}_PROVIDER", null);
            Environment.SetEnvironmentVariable($"{prefix}_TEST_TOKEN", null);
            Environment.SetEnvironmentVariable($"{prefix}_TEST_BASE_URL", null);
        }
    }

    [Fact]
    public void FromYaml_ConfiguresProvider()
    {
        var factory = new FakeFactory("test");
        var builder = new VcsProviderBuilder()
            .RegisterProvider(factory)
            .FromYaml("""
                provider:
                  test:
                    token: abc
                    base:
                      url: https://example.test
                """);

        var provider = builder.Build();

        Assert.NotNull(provider);
        Assert.Equal("abc", factory.LastConfiguration?["token"]);
        Assert.Equal("https://example.test", factory.LastConfiguration?["base.url"]);
    }

    [Fact]
    public void FromYamlFile_ConfiguresProvider()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, """
            provider:
              test:
                token: abc
            """);

        try
        {
            var factory = new FakeFactory("test");
            var builder = new VcsProviderBuilder()
                .RegisterProvider(factory)
                .FromYamlFile(path);

            var provider = builder.Build();

            Assert.NotNull(provider);
            Assert.Equal("abc", factory.LastConfiguration?["token"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Build_AppliesLayersInRegistrationOrder()
    {
        var calls = new List<string>();
        var builder = new VcsProviderBuilder()
            .RegisterProvider(new FakeFactory("test"))
            .UseProvider("test")
            .AddLayer(new TrackingLayer("first", calls))
            .AddLayer(new TrackingLayer("second", calls));

        await using var provider = builder.Build();
        await provider.RepositoryExistsAsync(new RepositoryRef { Name = "repo" });

        Assert.Equal(
            new[] { "first-enter", "second-enter", "second-exit", "first-exit" },
            calls);
    }

    private sealed class FakeFactory(string providerName) : IVcsProviderFactory
    {
        public string ProviderName { get; } = providerName;

        public IReadOnlyDictionary<string, string>? LastConfiguration { get; private set; }

        public IVcsProvider Create(IReadOnlyDictionary<string, string> configuration)
        {
            LastConfiguration = configuration;
            return new FakeVcsProvider();
        }
    }

    private sealed class TrackingLayer(string name, List<string> calls) : IVcsProviderLayer
    {
        public IVcsProvider Wrap(IVcsProvider inner)
        {
            return new TrackingProvider(inner, name, calls);
        }

        private sealed class TrackingProvider(IVcsProvider inner, string name, List<string> calls)
            : VcsProviderDecorator(inner)
        {
            public override async Task<bool> RepositoryExistsAsync(
                RepositoryRef repository,
                CancellationToken cancellationToken = default)
            {
                calls.Add($"{name}-enter");
                var result = await base.RepositoryExistsAsync(repository, cancellationToken);
                calls.Add($"{name}-exit");
                return result;
            }
        }
    }
}
