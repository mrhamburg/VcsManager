namespace VcsManager.AzureDevOps.Internal;

internal interface IAzureDevOpsApiClient : IAsyncDisposable
{
    Task<AzureDevOpsRepository> CreateRepositoryAsync(
        string project,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken);

    Task<AzureDevOpsRepository> GetRepositoryAsync(
        string project,
        string name,
        CancellationToken cancellationToken);

    Task DeleteRepositoryAsync(
        string project,
        string name,
        CancellationToken cancellationToken);

    Task<AzureDevOpsPage<AzureDevOpsRepository>> ListRepositoriesAsync(
        string? project,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken);

    Task<AzureDevOpsPullRequest> CreatePullRequestAsync(
        string project,
        string repositoryName,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken);

    Task<AzureDevOpsPullRequest> GetPullRequestAsync(
        string project,
        string repositoryName,
        int number,
        CancellationToken cancellationToken);

    Task<AzureDevOpsPage<AzureDevOpsPullRequest>> ListPullRequestsAsync(
        string project,
        string repositoryName,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken);

    Task<AzureDevOpsPullRequest> CompletePullRequestAsync(
        string project,
        string repositoryName,
        int number,
        string sourceCommitId,
        MergePullRequestRequest request,
        CancellationToken cancellationToken);

    Task<AzureDevOpsPullRequest> AbandonPullRequestAsync(
        string project,
        string repositoryName,
        int number,
        CancellationToken cancellationToken);
}
