using VcsManager.GitHub.Internal;

namespace VcsManager.GitHub.Tests;

internal static class TestData
{
    public static GitHubRepository Repository(
        string owner = "test-owner",
        string name = "test-repo",
        VcsVisibility visibility = VcsVisibility.Private,
        bool archived = false)
    {
        return new GitHubRepository
        {
            Id = 42,
            Name = name,
            Owner = owner,
            FullName = $"{owner}/{name}",
            HtmlUrl = $"https://github.test/{owner}/{name}",
            DefaultBranch = "main",
            Visibility = visibility,
            Description = "Test repository",
            IsArchived = archived,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };
    }

    public static GitHubPullRequest PullRequest(
        int number = 7,
        PullRequestState state = PullRequestState.Open,
        bool sourceBranchIsFromFork = false)
    {
        return new GitHubPullRequest
        {
            Id = 100 + number,
            Number = number,
            Title = "Test pull request",
            Description = "A test pull request",
            SourceBranch = "feature",
            TargetBranch = "main",
            State = state,
            IsDraft = state == PullRequestState.Draft,
            Author = "test-user",
            HtmlUrl = $"https://github.test/test-owner/test-repo/pull/{number}",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            MergedAt = state == PullRequestState.Merged ? DateTimeOffset.UnixEpoch : null,
            ClosedAt = state == PullRequestState.Closed ? DateTimeOffset.UnixEpoch : null,
            SourceBranchIsFromFork = sourceBranchIsFromFork,
        };
    }

    public static GitHubPage<GitHubRepository> RepositoryPage(
        IReadOnlyList<GitHubRepository> items,
        bool hasNextPage = false)
    {
        return new GitHubPage<GitHubRepository>(items, hasNextPage);
    }

    public static GitHubPage<GitHubPullRequest> PullRequestPage(
        IReadOnlyList<GitHubPullRequest> items,
        bool hasNextPage = false)
    {
        return new GitHubPage<GitHubPullRequest>(items, hasNextPage);
    }
}
