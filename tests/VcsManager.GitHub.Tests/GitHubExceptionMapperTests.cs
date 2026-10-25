using System.Net;
using Octokit;
using VcsManager.GitHub.Internal;

namespace VcsManager.GitHub.Tests;

public sealed class GitHubExceptionMapperTests
{
    [Fact]
    public void MapRepository_NotFound_MapsToRepositoryNotFoundException()
    {
        var exception = new ApiException("Not Found", HttpStatusCode.NotFound);

        var mapped = GitHubExceptionMapper.MapRepository(
            exception,
            "GetRepository",
            "test-owner",
            "test-repo");

        var notFound = Assert.IsType<RepositoryNotFoundException>(mapped);
        Assert.Equal("test-owner", notFound.Owner);
        Assert.Equal("test-repo", notFound.Name);
        Assert.Same(exception, notFound.InnerException);
    }

    [Fact]
    public void MapPullRequest_NotFound_MapsToPullRequestNotFoundException()
    {
        var exception = new ApiException("Not Found", HttpStatusCode.NotFound);
        var repository = new RepositoryRef { Owner = "test-owner", Name = "test-repo" };

        var mapped = GitHubExceptionMapper.MapPullRequest(
            exception,
            "GetPullRequest",
            repository,
            7);

        var notFound = Assert.IsType<PullRequestNotFoundException>(mapped);
        Assert.Equal(7, notFound.Number);
        Assert.Same(exception, notFound.InnerException);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(VcsAuthenticationException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(VcsAuthorizationException))]
    [InlineData(HttpStatusCode.MethodNotAllowed, typeof(VcsUnsupportedOperationException))]
    [InlineData(HttpStatusCode.Conflict, typeof(VcsConflictException))]
    [InlineData(HttpStatusCode.UnprocessableEntity, typeof(VcsConflictException))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(VcsRateLimitException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(VcsProviderException))]
    public void MapRepository_MapsStatusCodes(HttpStatusCode statusCode, Type expectedType)
    {
        var exception = new ApiException("Failure", statusCode);

        var mapped = GitHubExceptionMapper.MapRepository(
            exception,
            "GetRepository",
            "test-owner",
            "test-repo");

        Assert.IsType(expectedType, mapped);
        Assert.Same(exception, mapped.InnerException);

        if (expectedType == typeof(VcsProviderException) && (int)statusCode >= 500)
        {
            Assert.True(((VcsProviderException)mapped).IsTransient);
        }
    }

    [Fact]
    public void MapOperation_NetworkFailure_MapsToProviderException()
    {
        var exception = new HttpRequestException("Network failure");

        var mapped = GitHubExceptionMapper.MapOperation(exception, "ListRepositories");

        var providerException = Assert.IsType<VcsProviderException>(mapped);
        Assert.True(providerException.IsTransient);
        Assert.Same(exception, mapped.InnerException);
    }

    [Theory]
    [InlineData(typeof(TaskCanceledException))]
    [InlineData(typeof(OperationCanceledException))]
    public void MapOperation_RequestTimeout_MapsToTransientProviderException(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "A timeout occurred")!;

        var mapped = GitHubExceptionMapper.MapOperation(exception, "ListRepositories");

        var providerException = Assert.IsType<VcsProviderException>(mapped);
        Assert.True(providerException.IsTransient);
        Assert.Contains("timed out", providerException.Message);
        Assert.Same(exception, providerException.InnerException);
    }

    [Fact]
    public void MapOperation_VcsException_IsNotRewrapped()
    {
        var exception = new VcsConflictException("Already mapped");

        var mapped = GitHubExceptionMapper.MapOperation(exception, "MergePullRequest");

        Assert.Same(exception, mapped);
    }

    [Fact]
    public void MapBranch_NotFound_MapsToBranchNotFoundException()
    {
        var exception = new ApiException("Not Found", HttpStatusCode.NotFound);

        var mapped = GitHubExceptionMapper.MapBranch(
            exception,
            "DeleteSourceBranch",
            "test-owner",
            "test-repo",
            "feature");

        var notFound = Assert.IsType<VcsNotFoundException>(mapped);
        Assert.Equal("branch", notFound.Resource);
        Assert.Same(exception, notFound.InnerException);
    }
}
