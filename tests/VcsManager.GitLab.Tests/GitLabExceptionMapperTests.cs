using System.Net;
using VcsManager.GitLab.Internal;

namespace VcsManager.GitLab.Tests;

public sealed class GitLabExceptionMapperTests
{
    private static readonly RepositoryRef Repository =
        new() { Owner = "group", Project = "subgroup", Name = "project" };

    [Fact]
    public void MapRepository_NotFound_MapsToRepositoryNotFoundException()
    {
        var exception = ApiException(HttpStatusCode.NotFound);

        var mapped = GitLabExceptionMapper.MapRepository(
            exception,
            "GetRepository",
            Repository);

        var notFound = Assert.IsType<RepositoryNotFoundException>(mapped);
        Assert.Equal("group", notFound.Owner);
        Assert.Equal("subgroup", notFound.Project);
        Assert.Equal("project", notFound.Name);
        Assert.Same(exception, notFound.InnerException);
    }

    [Fact]
    public void MapPullRequest_NotFound_MapsToPullRequestNotFoundException()
    {
        var exception = ApiException(HttpStatusCode.NotFound);

        var mapped = GitLabExceptionMapper.MapPullRequest(
            exception,
            "GetPullRequest",
            Repository,
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
    [InlineData(HttpStatusCode.ServiceUnavailable, typeof(VcsProviderException))]
    public void MapRepository_MapsStatusCodes(HttpStatusCode statusCode, Type expectedType)
    {
        var exception = ApiException(statusCode);

        var mapped = GitLabExceptionMapper.MapRepository(
            exception,
            "GetRepository",
            Repository);

        Assert.IsType(expectedType, mapped);
        Assert.Same(exception, mapped.InnerException);
    }

    [Fact]
    public void MapRepository_DuplicateProject_MapsToConflict()
    {
        var exception = ApiException(
            HttpStatusCode.BadRequest,
            "{\"message\":{\"path\":[\"has already been taken\"]}}");

        var mapped = GitLabExceptionMapper.MapRepository(
            exception,
            "CreateRepository",
            Repository);

        Assert.IsType<VcsConflictException>(mapped);
    }

    [Fact]
    public void MapRepository_ValidationError_MapsToProviderException()
    {
        var exception = ApiException(HttpStatusCode.BadRequest, "Invalid project name");

        var mapped = GitLabExceptionMapper.MapRepository(
            exception,
            "CreateRepository",
            Repository);

        Assert.IsType<VcsProviderException>(mapped);
    }

    [Fact]
    public void MapRepository_RateLimit_PreservesRetryAfter()
    {
        var retryAfter = TimeSpan.FromSeconds(30);
        var exception = new GitLabApiException(
            HttpStatusCode.TooManyRequests,
            "Rate limit exceeded",
            retryAfter);

        var mapped = GitLabExceptionMapper.MapRepository(
            exception,
            "GetRepository",
            Repository);

        var rateLimit = Assert.IsType<VcsRateLimitException>(mapped);
        Assert.Equal(retryAfter, rateLimit.RetryAfter);
    }

    [Fact]
    public void MapRepository_ForbiddenWithRetryAfter_MapsToRateLimit()
    {
        var retryAfter = TimeSpan.FromSeconds(10);
        var exception = new GitLabApiException(
            HttpStatusCode.Forbidden,
            "Rate limit exceeded",
            retryAfter);

        var mapped = GitLabExceptionMapper.MapRepository(
            exception,
            "GetRepository",
            Repository);

        var rateLimit = Assert.IsType<VcsRateLimitException>(mapped);
        Assert.Equal(retryAfter, rateLimit.RetryAfter);
    }

    [Fact]
    public void MapOperation_NetworkFailure_MapsToProviderException()
    {
        var exception = new HttpRequestException("Network failure");

        var mapped = GitLabExceptionMapper.MapOperation(exception, "ListRepositories");

        Assert.IsType<VcsProviderException>(mapped);
        Assert.Same(exception, mapped.InnerException);
    }

    [Fact]
    public void MapOperation_VcsException_IsNotRewrapped()
    {
        var exception = new VcsConflictException("Already mapped");

        var mapped = GitLabExceptionMapper.MapOperation(exception, "MergePullRequest");

        Assert.Same(exception, mapped);
    }

    private static GitLabApiException ApiException(
        HttpStatusCode statusCode,
        string body = "GitLab API error")
    {
        return new GitLabApiException(statusCode, body, retryAfter: null);
    }
}
