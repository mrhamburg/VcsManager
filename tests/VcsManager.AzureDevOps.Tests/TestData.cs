using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps.Tests;

internal static class TestData
{
    public const string Organization = "test-organization";
    public const string Project = "test-project";

    public static readonly Guid RepositoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static AzureDevOpsRepository Repository(
        string name = "test-repository",
        string project = Project,
        string? defaultBranch = "refs/heads/main")
    {
        return new AzureDevOpsRepository
        {
            Id = RepositoryId,
            Name = name,
            Project = project,
            Organization = Organization,
            WebUrl = $"https://dev.azure.com/{Organization}/{project}/_git/{name}",
            DefaultBranch = defaultBranch,
            CreatedAt = DateTimeOffset.UnixEpoch,
        };
    }

    public static AzureDevOpsPullRequest PullRequest(
        int number = 7,
        PullRequestState state = PullRequestState.Open,
        bool draft = false)
    {
        return new AzureDevOpsPullRequest
        {
            Id = number,
            Number = number,
            Title = "Test pull request",
            Description = "A test pull request",
            SourceBranch = "feature",
            TargetBranch = "main",
            State = state,
            IsDraft = draft,
            Author = "Test User",
            WebUrl = $"https://dev.azure.com/{Organization}/{Project}/_git/test-repository/pullrequest/{number}",
            SourceCommitId = "2222222222222222222222222222222222222222",
            CreatedAt = DateTimeOffset.UnixEpoch,
            MergedAt = state == PullRequestState.Merged ? DateTimeOffset.UnixEpoch : null,
            ClosedAt = state == PullRequestState.Closed ? DateTimeOffset.UnixEpoch : null,
        };
    }

    public static AzureDevOpsPage<AzureDevOpsRepository> RepositoryPage(
        IReadOnlyList<AzureDevOpsRepository> items,
        bool hasNextPage = false,
        int? totalCount = null)
    {
        return new AzureDevOpsPage<AzureDevOpsRepository>(items, hasNextPage, totalCount);
    }

    public static AzureDevOpsPage<AzureDevOpsPullRequest> PullRequestPage(
        IReadOnlyList<AzureDevOpsPullRequest> items,
        bool hasNextPage = false,
        int? totalCount = null)
    {
        return new AzureDevOpsPage<AzureDevOpsPullRequest>(items, hasNextPage, totalCount);
    }
}
