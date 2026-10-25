namespace VcsManager;

/// <summary>
/// Defines the operations that every version control provider implements.
/// </summary>
public interface IVcsProvider : IAsyncDisposable
{
    /// <summary>
    /// Creates a repository.
    /// </summary>
    /// <param name="request">The repository to create.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The created repository.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="VcsAuthenticationException">The credentials are missing or invalid.</exception>
    /// <exception cref="VcsAuthorizationException">The credentials do not permit repository creation.</exception>
    /// <exception cref="VcsConflictException">A repository with the same address already exists.</exception>
    /// <exception cref="VcsRateLimitException">The provider rate limit is exceeded.</exception>
    /// <exception cref="VcsUnsupportedOperationException">The provider cannot create a repository with the requested options.</exception>
    /// <exception cref="VcsProviderException">The provider returns another failure.</exception>
    Task<VcsRepository> CreateRepositoryAsync(
        CreateRepositoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds an empty repository with one initial commit from a file set.
    /// </summary>
    /// <param name="repository">The empty repository to seed.</param>
    /// <param name="request">The initial commit to create.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task that completes when the initial commit exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The request is invalid.</exception>
    /// <exception cref="RepositoryNotFoundException">The repository does not exist.</exception>
    /// <exception cref="VcsAuthenticationException">The credentials are missing or invalid.</exception>
    /// <exception cref="VcsAuthorizationException">The credentials do not permit writing to the repository.</exception>
    /// <exception cref="VcsConflictException">The repository already has a commit or the branch already exists.</exception>
    /// <exception cref="VcsRateLimitException">The provider rate limit is exceeded.</exception>
    /// <exception cref="VcsUnsupportedOperationException">The provider cannot seed the repository.</exception>
    /// <exception cref="VcsProviderException">The provider returns another failure.</exception>
    Task SeedRepositoryAsync(
        RepositoryRef repository,
        SeedRepositoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one repository. This is a supporting operation.
    /// </summary>
    /// <param name="repository">The repository to get.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The repository.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="RepositoryNotFoundException">The repository does not exist.</exception>
    /// <exception cref="VcsAuthenticationException">The credentials are missing or invalid.</exception>
    /// <exception cref="VcsAuthorizationException">The credentials do not permit reading the repository.</exception>
    /// <exception cref="VcsRateLimitException">The provider rate limit is exceeded.</exception>
    /// <exception cref="VcsProviderException">The provider returns another failure.</exception>
    Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists repositories in the configured scope.
    /// </summary>
    /// <param name="query">The filters to apply, or <see langword="null"/> for the default scope.</param>
    /// <param name="pagination">The page to request, or <see langword="null"/> for the default page.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>One page of repositories.</returns>
    /// <exception cref="VcsAuthenticationException">The credentials are missing or invalid.</exception>
    /// <exception cref="VcsAuthorizationException">The credentials do not permit listing repositories.</exception>
    /// <exception cref="VcsRateLimitException">The provider rate limit is exceeded.</exception>
    /// <exception cref="VcsProviderException">The provider returns another failure.</exception>
    Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a repository.
    /// </summary>
    /// <param name="repository">The repository to delete.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task that completes when the repository is deleted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="RepositoryNotFoundException">The repository does not exist.</exception>
    /// <exception cref="VcsAuthenticationException">The credentials are missing or invalid.</exception>
    /// <exception cref="VcsAuthorizationException">The credentials do not permit deleting the repository.</exception>
    /// <exception cref="VcsConflictException">The repository cannot be deleted in its current state.</exception>
    /// <exception cref="VcsRateLimitException">The provider rate limit is exceeded.</exception>
    /// <exception cref="VcsProviderException">The provider returns another failure.</exception>
    Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a repository exists. This is a supporting operation.
    /// </summary>
    /// <param name="repository">The repository to check.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns><see langword="true"/> when the repository exists; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="VcsAuthenticationException">The credentials are missing or invalid.</exception>
    /// <exception cref="VcsAuthorizationException">The credentials do not permit reading the repository.</exception>
    /// <exception cref="VcsRateLimitException">The provider rate limit is exceeded.</exception>
    /// <exception cref="VcsProviderException">The provider returns another failure.</exception>
    Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a pull request.
    /// </summary>
    /// <param name="repository">The repository that receives the pull request.</param>
    /// <param name="request">The pull request to create.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The created pull request.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="RepositoryNotFoundException">The repository does not exist.</exception>
    /// <exception cref="VcsAuthenticationException">The credentials are missing or invalid.</exception>
    /// <exception cref="VcsAuthorizationException">The credentials do not permit creating a pull request.</exception>
    /// <exception cref="VcsConflictException">The branches or pull request state conflict with the request.</exception>
    /// <exception cref="VcsRateLimitException">The provider rate limit is exceeded.</exception>
    /// <exception cref="VcsUnsupportedOperationException">The provider cannot create the requested pull request.</exception>
    /// <exception cref="VcsProviderException">The provider returns another failure.</exception>
    Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one pull request.
    /// </summary>
    /// <param name="repository">The repository that contains the pull request.</param>
    /// <param name="number">The pull request number.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The pull request.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="PullRequestNotFoundException">The pull request does not exist.</exception>
    /// <exception cref="VcsAuthenticationException">The credentials are missing or invalid.</exception>
    /// <exception cref="VcsAuthorizationException">The credentials do not permit reading the pull request.</exception>
    /// <exception cref="VcsRateLimitException">The provider rate limit is exceeded.</exception>
    /// <exception cref="VcsProviderException">The provider returns another failure.</exception>
    Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists pull requests in a repository. This is a supporting operation.
    /// </summary>
    /// <param name="repository">The repository that contains the pull requests.</param>
    /// <param name="query">The filters to apply, or <see langword="null"/> for all pull requests.</param>
    /// <param name="pagination">The page to request, or <see langword="null"/> for the default page.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>One page of pull requests.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="RepositoryNotFoundException">The repository does not exist.</exception>
    /// <exception cref="VcsAuthenticationException">The credentials are missing or invalid.</exception>
    /// <exception cref="VcsAuthorizationException">The credentials do not permit listing pull requests.</exception>
    /// <exception cref="VcsRateLimitException">The provider rate limit is exceeded.</exception>
    /// <exception cref="VcsProviderException">The provider returns another failure.</exception>
    Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges a pull request. Failed merges are not retried automatically.
    /// </summary>
    /// <param name="repository">The repository that contains the pull request.</param>
    /// <param name="number">The pull request number.</param>
    /// <param name="request">The merge options, or <see langword="null"/> for the default method.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The merged pull request.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="PullRequestNotFoundException">The pull request does not exist.</exception>
    /// <exception cref="VcsAuthenticationException">The credentials are missing or invalid.</exception>
    /// <exception cref="VcsAuthorizationException">The credentials do not permit merging the pull request.</exception>
    /// <exception cref="VcsConflictException">The merge conflicts or the source commit changed.</exception>
    /// <exception cref="VcsRateLimitException">The provider rate limit is exceeded.</exception>
    /// <exception cref="VcsUnsupportedOperationException">The provider cannot perform the requested merge method for the current settings.</exception>
    /// <exception cref="VcsProviderException">The provider returns another failure.</exception>
    Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a pull request without merging it. This operation is idempotent.
    /// </summary>
    /// <param name="repository">The repository that contains the pull request.</param>
    /// <param name="number">The pull request number.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The pull request in its current state. A closed or merged pull request is returned unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="PullRequestNotFoundException">The pull request does not exist.</exception>
    /// <exception cref="VcsAuthenticationException">The credentials are missing or invalid.</exception>
    /// <exception cref="VcsAuthorizationException">The credentials do not permit closing the pull request.</exception>
    /// <exception cref="VcsRateLimitException">The provider rate limit is exceeded.</exception>
    /// <exception cref="VcsProviderException">The provider returns another failure.</exception>
    Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default);
}
