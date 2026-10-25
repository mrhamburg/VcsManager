using VcsManager.GitLab.Internal;

namespace VcsManager.GitLab;

/// <summary>
/// GitLab implementation of <see cref="IVcsProvider"/>.
/// </summary>
public sealed class GitLabProvider : IVcsProvider
{
    private readonly IGitLabApiClient _client;
    private readonly GitLabOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitLabProvider"/> class.
    /// </summary>
    /// <param name="options">The GitLab options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The options are invalid.</exception>
    public GitLabProvider(GitLabOptions options)
        : this(CreateClient(options), options)
    {
    }

    internal GitLabProvider(IGitLabApiClient client, GitLabOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        _client = client;
        _options = options;
    }

    /// <inheritdoc/>
    public Task<VcsRepository> CreateRepositoryAsync(
        CreateRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab repository operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab repository operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab repository operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab repository operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab repository operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab merge request operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab merge request operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab merge request operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab merge request operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab merge request operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }

    private static IGitLabApiClient CreateClient(GitLabOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return new GitLabApiClient(options);
    }
}
