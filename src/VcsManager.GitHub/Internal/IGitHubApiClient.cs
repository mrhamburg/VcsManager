namespace VcsManager.GitHub.Internal;

internal interface IGitHubApiClient : IAsyncDisposable
{
    Task<GitHubRepository> CreateRepositoryAsync(
        string? owner,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken);

    Task<GitHubRepository> GetRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken);

    Task DeleteRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken);

    Task<GitHubPage<GitHubRepository>> ListRepositoriesAsync(
        string? owner,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken);

    Task<GitHubPullRequest> CreatePullRequestAsync(
        string owner,
        string name,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken);

    Task<GitHubPullRequest> GetPullRequestAsync(
        string owner,
        string name,
        int number,
        CancellationToken cancellationToken);

    Task<GitHubPage<GitHubPullRequest>> ListPullRequestsAsync(
        string owner,
        string name,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken);

    Task<GitHubPullRequest> MergePullRequestAsync(
        string owner,
        string name,
        int number,
        MergePullRequestRequest request,
        CancellationToken cancellationToken);

    Task<GitHubPullRequest> ClosePullRequestAsync(
        string owner,
        string name,
        int number,
        CancellationToken cancellationToken);

    Task DeleteBranchAsync(
        string owner,
        string name,
        string branch,
        CancellationToken cancellationToken);
}
