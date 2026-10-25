namespace VcsManager.GitLab.Internal;

internal interface IGitLabApiClient : IAsyncDisposable
{
    Task<GitLabProject> CreateProjectAsync(
        string? @namespace,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken);

    Task SeedProjectAsync(
        string projectPath,
        SeedRepositoryRequest request,
        CancellationToken cancellationToken);

    Task<GitLabProject> GetProjectAsync(
        string projectPath,
        CancellationToken cancellationToken);

    Task DeleteProjectAsync(
        string projectPath,
        CancellationToken cancellationToken);

    Task<GitLabPage<GitLabProject>> ListProjectsAsync(
        string? @namespace,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken);

    Task<GitLabMergeRequest> CreateMergeRequestAsync(
        string projectPath,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken);

    Task<GitLabMergeRequest> GetMergeRequestAsync(
        string projectPath,
        int iid,
        CancellationToken cancellationToken);

    Task<GitLabPage<GitLabMergeRequest>> ListMergeRequestsAsync(
        string projectPath,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken);

    Task<GitLabMergeRequest> MergeMergeRequestAsync(
        string projectPath,
        int iid,
        MergePullRequestRequest request,
        CancellationToken cancellationToken);

    Task<GitLabMergeRequest> CloseMergeRequestAsync(
        string projectPath,
        int iid,
        CancellationToken cancellationToken);
}
