using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaExceptionMapperTests
{
    [Fact]
    public void MapOperation_TimedOutRequest_MapsToTransientProviderException()
    {
        var exception = new TaskCanceledException(
            "The request was canceled due to the configured HttpClient.Timeout.",
            new TimeoutException());

        var mapped = GiteaExceptionMapper.MapOperation(exception, "ListRepositories");

        var providerException = Assert.IsType<VcsProviderException>(mapped);
        Assert.True(providerException.IsTransient);
        Assert.Contains("timed out", providerException.Message, StringComparison.Ordinal);
        Assert.Same(exception, providerException.InnerException);
    }
}
