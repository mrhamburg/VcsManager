using VcsManager.Builder;

namespace VcsManager.Tests;

public class LayerExtensionsTests
{
    [Fact]
    public void EachExtensionReturnsTheSameBuilder()
    {
        var builder = new VcsProviderBuilder();

        Assert.Same(builder, builder.WithRetry());
        Assert.Same(builder, builder.WithTimeout());
        Assert.Same(builder, builder.WithConcurrentLimiter());
        Assert.Same(builder, builder.WithLogging());
        Assert.Same(builder, builder.WithOtelMetrics());
        Assert.Same(builder, builder.WithOtelTrace());
    }

    [Fact]
    public async Task AllLayersCanBeAddedThroughTheBuilder()
    {
        var builder = new VcsProviderBuilder()
            .RegisterProvider(new Factory("test"))
            .UseProvider("test")
            .WithRetry(new RetryLayerOptions { MaxAttempts = 1 })
            .WithTimeout(new TimeoutLayerOptions { Timeout = TimeSpan.FromSeconds(5) })
            .WithConcurrentLimiter(new ConcurrentLimiterLayerOptions { MaxConcurrentRequests = 2 })
            .WithLogging()
            .WithOtelMetrics()
            .WithOtelTrace();

        await using var provider = builder.Build();

        Assert.False(await provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" }));
    }

    private sealed class Factory(string providerName) : IVcsProviderFactory
    {
        public string ProviderName { get; } = providerName;

        public IVcsProvider Create(IReadOnlyDictionary<string, string> configuration)
        {
            return new FakeVcsProvider();
        }
    }
}
