namespace VcsManager;

/// <summary>
/// Base class for provider layers. Every method forwards to the inner provider.
/// Layers are applied in registration order by <c>VcsProviderBuilder</c>, so the
/// first registered layer is the outermost wrapper.
/// </summary>
public abstract class VcsProviderDecorator : IVcsProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VcsProviderDecorator"/> class.
    /// </summary>
    /// <param name="inner">The provider to wrap.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inner"/> is <see langword="null"/>.</exception>
    protected VcsProviderDecorator(IVcsProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        Inner = inner;
    }

    /// <summary>
    /// Gets the wrapped provider.
    /// </summary>
    protected IVcsProvider Inner { get; }

    /// <inheritdoc/>
    public virtual Task<VcsRepository> CreateRepositoryAsync(
        CreateRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        return Inner.CreateRepositoryAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        return Inner.GetRepositoryAsync(repository, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        return Inner.ListRepositoriesAsync(query, pagination, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        return Inner.DeleteRepositoryAsync(repository, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        return Inner.RepositoryExistsAsync(repository, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        return Inner.CreatePullRequestAsync(repository, request, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        return Inner.GetPullRequestAsync(repository, number, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        return Inner.ListPullRequestsAsync(repository, query, pagination, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        return Inner.MergePullRequestAsync(repository, number, request, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        return Inner.ClosePullRequestAsync(repository, number, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual ValueTask DisposeAsync()
    {
        return Inner.DisposeAsync();
    }
}
