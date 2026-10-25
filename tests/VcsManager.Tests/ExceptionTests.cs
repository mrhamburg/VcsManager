namespace VcsManager.Tests;

public class ExceptionTests
{
    [Fact]
    public void ProviderException_PreservesMessageAndInnerException()
    {
        var inner = new InvalidOperationException("provider failure");

        var exception = new VcsProviderException("operation failed", inner);

        Assert.Equal("operation failed", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void NotFoundException_ExposesResource()
    {
        var exception = new VcsNotFoundException("branch");

        Assert.Equal("branch", exception.Resource);
        Assert.Contains("branch", exception.Message);
        Assert.IsAssignableFrom<VcsProviderException>(exception);
    }

    [Fact]
    public void NotFoundException_PreservesCustomMessageAndInnerException()
    {
        var inner = new InvalidOperationException("provider failure");

        var exception = new VcsNotFoundException("branch", "branch 'main' was not found", inner);

        Assert.Equal("branch", exception.Resource);
        Assert.Equal("branch 'main' was not found", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void RepositoryNotFoundException_ExposesContext()
    {
        var exception = new RepositoryNotFoundException("acme", "platform", "core");

        Assert.Equal("acme", exception.Owner);
        Assert.Equal("platform", exception.Project);
        Assert.Equal("core", exception.Name);
        Assert.Equal("repository", exception.Resource);
        Assert.Contains("acme/platform/core", exception.Message);
    }

    [Fact]
    public void RepositoryNotFoundException_OmitsMissingAddressParts()
    {
        var exception = new RepositoryNotFoundException(null, null, "core");

        Assert.Null(exception.Owner);
        Assert.Null(exception.Project);
        Assert.Equal("core", exception.Name);
        Assert.Contains("'core'", exception.Message);
    }

    [Fact]
    public void RepositoryNotFoundException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("provider failure");

        var exception = new RepositoryNotFoundException("acme", null, "core", inner);

        Assert.Same(inner, exception.InnerException);
        Assert.Contains("acme/core", exception.Message);
    }

    [Fact]
    public void PullRequestNotFoundException_ExposesContext()
    {
        var repository = new RepositoryRef { Owner = "acme", Project = "platform", Name = "core" };

        var exception = new PullRequestNotFoundException(repository, 42);

        Assert.Same(repository, exception.Repository);
        Assert.Equal(42, exception.Number);
        Assert.Equal("pull request", exception.Resource);
        Assert.Contains("#42", exception.Message);
        Assert.Contains("acme/platform/core", exception.Message);
    }

    [Fact]
    public void PullRequestNotFoundException_AcceptsAddressParts()
    {
        var exception = new PullRequestNotFoundException("acme", null, "core", 7);

        Assert.Equal("acme", exception.Repository.Owner);
        Assert.Null(exception.Repository.Project);
        Assert.Equal("core", exception.Repository.Name);
        Assert.Equal(7, exception.Number);
        Assert.Contains("acme/core", exception.Message);
    }

    [Fact]
    public void PullRequestNotFoundException_PreservesInnerException()
    {
        var repository = new RepositoryRef { Owner = "acme", Name = "core" };
        var inner = new InvalidOperationException("provider failure");

        var exception = new PullRequestNotFoundException(repository, 42, inner);

        Assert.Same(inner, exception.InnerException);
        Assert.Equal(42, exception.Number);
    }

    [Fact]
    public void AuthenticationException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("provider failure");

        var exception = new VcsAuthenticationException("token is invalid", inner);

        Assert.Equal("token is invalid", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void AuthorizationException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("provider failure");

        var exception = new VcsAuthorizationException("permission denied", inner);

        Assert.Equal("permission denied", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void RateLimitException_AcceptsNullRetryAfter()
    {
        var exception = new VcsRateLimitException("rate limit exceeded", null);

        Assert.Null(exception.RetryAfter);
    }

    [Fact]
    public void RateLimitException_ExposesRetryAfterAndInnerException()
    {
        var inner = new InvalidOperationException("provider failure");
        var retryAfter = TimeSpan.FromSeconds(30);

        var exception = new VcsRateLimitException("rate limit exceeded", retryAfter, inner);

        Assert.Equal(retryAfter, exception.RetryAfter);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void ConflictException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("provider failure");

        var exception = new VcsConflictException("source commit changed", inner);

        Assert.Equal("source commit changed", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void UnsupportedOperationException_ExposesProviderAndOperation()
    {
        var exception = new VcsUnsupportedOperationException("GitLab", "Rebase");

        Assert.Equal("GitLab", exception.Provider);
        Assert.Equal("Rebase", exception.Operation);
        Assert.Contains("GitLab", exception.Message);
        Assert.Contains("Rebase", exception.Message);
    }

    [Fact]
    public void UnsupportedOperationException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("provider failure");

        var exception = new VcsUnsupportedOperationException("AzureDevOps", "Rebase", inner);

        Assert.Same(inner, exception.InnerException);
        Assert.Equal("AzureDevOps", exception.Provider);
        Assert.Equal("Rebase", exception.Operation);
    }
}
