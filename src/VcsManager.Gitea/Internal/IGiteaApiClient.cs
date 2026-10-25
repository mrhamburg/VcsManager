namespace VcsManager.Gitea.Internal;

internal interface IGiteaApiClient : IAsyncDisposable
{
    Task<GiteaRepository> CreateRepositoryAsync(
        string owner,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken);

    Task<GiteaRepository> GetRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken);

    Task DeleteRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken);

    Task<GiteaPage<GiteaRepository>> ListRepositoriesAsync(
        string? owner,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken);

    Task<GiteaPullRequest> CreatePullRequestAsync(
        string owner,
        string repositoryName,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken);

    Task<GiteaPullRequest> GetPullRequestAsync(
        string owner,
        string repositoryName,
        int number,
        CancellationToken cancellationToken);

    Task<GiteaPage<GiteaPullRequest>> ListPullRequestsAsync(
        string owner,
        string repositoryName,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken);

    Task<GiteaPullRequest> MergePullRequestAsync(
        string owner,
        string repositoryName,
        int number,
        MergePullRequestRequest request,
        CancellationToken cancellationToken);

    Task<GiteaPullRequest> ClosePullRequestAsync(
        string owner,
        string repositoryName,
        int number,
        CancellationToken cancellationToken);
}
