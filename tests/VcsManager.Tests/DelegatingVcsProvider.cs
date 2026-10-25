namespace VcsManager.Tests;

internal class DelegatingVcsProvider : IVcsProvider
{
    private readonly IVcsProvider _inner;

    protected DelegatingVcsProvider(IVcsProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public virtual Task<VcsRepository> CreateRepositoryAsync(
        CreateRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        return _inner.CreateRepositoryAsync(request, cancellationToken);
    }

    public virtual Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        return _inner.GetRepositoryAsync(repository, cancellationToken);
    }

    public virtual Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        return _inner.ListRepositoriesAsync(query, pagination, cancellationToken);
    }

    public virtual Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        return _inner.DeleteRepositoryAsync(repository, cancellationToken);
    }

    public virtual Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        return _inner.RepositoryExistsAsync(repository, cancellationToken);
    }

    public virtual Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        return _inner.CreatePullRequestAsync(repository, request, cancellationToken);
    }

    public virtual Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        return _inner.GetPullRequestAsync(repository, number, cancellationToken);
    }

    public virtual Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        return _inner.ListPullRequestsAsync(repository, query, pagination, cancellationToken);
    }

    public virtual Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        return _inner.MergePullRequestAsync(repository, number, request, cancellationToken);
    }

    public virtual Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        return _inner.ClosePullRequestAsync(repository, number, cancellationToken);
    }

    public virtual ValueTask DisposeAsync()
    {
        return _inner.DisposeAsync();
    }
}
