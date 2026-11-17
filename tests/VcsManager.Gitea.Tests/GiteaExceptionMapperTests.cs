using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaExceptionMapperTests
{

    [Fact]
    public void MapRepository_MethodNotAllowed_MapsToUnsupportedOperation()
    {
        var exception = new GiteaApiException(
            System.Net.HttpStatusCode.MethodNotAllowed,
            "method not allowed",
            retryAfter: null);

        var mapped = GiteaExceptionMapper.MapRepository(
            exception,
            "GetRepository",
            owner: "owner",
            name: "repo");

        Assert.IsType<VcsUnsupportedOperationException>(mapped);
    }

    [Fact]
    public void MapRepository_DoesNotIncludeProviderResponseBody()
    {
        var exception = new GiteaApiException(
            System.Net.HttpStatusCode.Unauthorized,
            "{\"token\":\"super-secret\"}",
            retryAfter: null);

        var mapped = GiteaExceptionMapper.MapRepository(
            exception,
            "GetRepository",
            owner: "owner",
            name: "repo");

        Assert.DoesNotContain("super-secret", mapped.Message, StringComparison.Ordinal);
    }

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
