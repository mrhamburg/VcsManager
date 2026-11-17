using VcsManager.Internal;

namespace VcsManager;

/// <summary>
/// Base class for provider layers. Every method forwards to the inner provider
/// through a single <see cref="ExecuteAsync{T}"/> hook, so a layer overrides
/// only the behavior it adds. Layers are applied in registration order by
/// <c>VcsProviderBuilder</c>, so the first registered layer is the outermost wrapper.
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

    /// <summary>
    /// Executes one provider operation. Override this method to add behavior.
    /// </summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="operation">The operation name.</param>
    /// <param name="repository">The repository display name, when applicable.</param>
    /// <param name="pullRequestNumber">The pull request number, when applicable.</param>
    /// <param name="call">The inner provider call.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The operation result.</returns>
    protected virtual Task<T> ExecuteAsync<T>(
        string operation,
        string? repository,
        int? pullRequestNumber,
        Func<CancellationToken, Task<T>> call,
        CancellationToken cancellationToken)
    {
        return call(cancellationToken);
    }

    /// <summary>
    /// Executes one provider operation that does not return a value.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="repository">The repository display name, when applicable.</param>
    /// <param name="pullRequestNumber">The pull request number, when applicable.</param>
    /// <param name="call">The inner provider call.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task that completes when the operation completes.</returns>
    protected virtual async Task ExecuteAsync(
        string operation,
        string? repository,
        int? pullRequestNumber,
        Func<CancellationToken, Task> call,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync<object?>(
            operation,
            repository,
            pullRequestNumber,
            async token =>
            {
                await call(token);
                return null;
            },
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<VcsRepository> CreateRepositoryAsync(
        CreateRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteAsync(
            VcsOperationNames.CreateRepository,
            RepositoryDisplayName.Format(request.Owner, request.Project, request.Name),
            pullRequestNumber: null,
            token => Inner.CreateRepositoryAsync(request, token),
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task SeedRepositoryAsync(
        RepositoryRef repository,
        SeedRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteAsync(
            VcsOperationNames.SeedRepository,
            RepositoryDisplayName.Format(repository),
            pullRequestNumber: null,
            token => Inner.SeedRepositoryAsync(repository, request, token),
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return ExecuteAsync(
            VcsOperationNames.GetRepository,
            RepositoryDisplayName.Format(repository),
            pullRequestNumber: null,
            token => Inner.GetRepositoryAsync(repository, token),
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            VcsOperationNames.ListRepositories,
            RepositoryDisplayName.FormatScope(query?.Owner, query?.Project),
            pullRequestNumber: null,
            token => Inner.ListRepositoriesAsync(query, pagination, token),
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return ExecuteAsync(
            VcsOperationNames.DeleteRepository,
            RepositoryDisplayName.Format(repository),
            pullRequestNumber: null,
            token => Inner.DeleteRepositoryAsync(repository, token),
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return ExecuteAsync(
            VcsOperationNames.RepositoryExists,
            RepositoryDisplayName.Format(repository),
            pullRequestNumber: null,
            token => Inner.RepositoryExistsAsync(repository, token),
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteAsync(
            VcsOperationNames.CreatePullRequest,
            RepositoryDisplayName.Format(repository),
            pullRequestNumber: null,
            token => Inner.CreatePullRequestAsync(repository, request, token),
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return ExecuteAsync(
            VcsOperationNames.GetPullRequest,
            RepositoryDisplayName.Format(repository),
            number,
            token => Inner.GetPullRequestAsync(repository, number, token),
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return ExecuteAsync(
            VcsOperationNames.ListPullRequests,
            RepositoryDisplayName.Format(repository),
            pullRequestNumber: null,
            token => Inner.ListPullRequestsAsync(repository, query, pagination, token),
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return ExecuteAsync(
            VcsOperationNames.MergePullRequest,
            RepositoryDisplayName.Format(repository),
            number,
            token => Inner.MergePullRequestAsync(repository, number, request, token),
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return ExecuteAsync(
            VcsOperationNames.ClosePullRequest,
            RepositoryDisplayName.Format(repository),
            number,
            token => Inner.ClosePullRequestAsync(repository, number, token),
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual ValueTask DisposeAsync()
    {
        return Inner.DisposeAsync();
    }
}
