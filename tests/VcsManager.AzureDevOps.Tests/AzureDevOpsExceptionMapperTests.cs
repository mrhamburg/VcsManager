using System.Net;
using System.Text.Json;
using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps.Tests;

public sealed class AzureDevOpsExceptionMapperTests
{
    [Fact]
    public void MapOperation_WithJsonException_ReturnsProviderException()
    {
        var inner = new JsonException("bad json");

        var mapped = AzureDevOpsExceptionMapper.MapOperation(inner, "ListRepositories");

        var exception = Assert.IsType<VcsProviderException>(mapped);
        Assert.Contains("The response was not valid JSON.", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("bad json", exception.Message, StringComparison.Ordinal);
        Assert.Same(inner, exception.InnerException);
    }

    [Theory]
    [InlineData(typeof(OperationCanceledException))]
    [InlineData(typeof(TaskCanceledException))]
    public void MapOperation_WithTimeoutCancellation_ReturnsTransientProviderException(
        Type exceptionType)
    {
        var inner = (Exception)Activator.CreateInstance(exceptionType)!;

        var mapped = AzureDevOpsExceptionMapper.MapOperation(inner, "ListRepositories");

        var exception = Assert.IsType<VcsProviderException>(mapped);
        Assert.True(exception.IsTransient);
        Assert.Contains("The request timed out.", exception.Message, StringComparison.Ordinal);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void MapOperation_WithUnauthorizedApiException_ReturnsAuthenticationException()
    {
        var mapped = AzureDevOpsExceptionMapper.MapOperation(
            new AzureDevOpsApiException(HttpStatusCode.Unauthorized, string.Empty),
            "ListRepositories");

        Assert.IsType<VcsAuthenticationException>(mapped);
    }
}
