using VcsManager.GitLab.Internal;

namespace VcsManager.GitLab.Tests;

internal static class TestData
{
    public static GitLabProject Project(
        string owner = "test-group",
        string? project = null,
        string name = "test-project",
        VcsVisibility visibility = VcsVisibility.Private,
        bool archived = false,
        bool deleted = false)
    {
        var segments = new[] { owner, project, name }
            .Where(segment => !string.IsNullOrWhiteSpace(segment));
        var path = string.Join("/", segments);

        return new GitLabProject
        {
            Id = 42,
            Name = name,
            Path = name,
            PathWithNamespace = path,
            WebUrl = $"https://gitlab.test/{path}",
            DefaultBranch = "main",
            Visibility = visibility,
            Description = "Test project",
            IsArchived = archived,
            IsDeleted = deleted,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            Owner = owner,
            Project = project,
        };
    }

    public static GitLabMergeRequest MergeRequest(
        int iid = 7,
        PullRequestState state = PullRequestState.Open)
    {
        return new GitLabMergeRequest
        {
            Id = 100 + iid,
            Iid = iid,
            Title = "Test merge request",
            Description = "A test merge request",
            SourceBranch = "feature",
            TargetBranch = "main",
            State = state,
            IsDraft = state == PullRequestState.Draft,
            Author = "test-user",
            WebUrl = $"https://gitlab.test/test-group/test-project/-/merge_requests/{iid}",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            MergedAt = state == PullRequestState.Merged ? DateTimeOffset.UnixEpoch : null,
            ClosedAt = state == PullRequestState.Closed ? DateTimeOffset.UnixEpoch : null,
        };
    }

    public static GitLabPage<GitLabProject> ProjectPage(
        IReadOnlyList<GitLabProject> items,
        bool hasNextPage = false,
        int? totalCount = null)
    {
        return new GitLabPage<GitLabProject>(items, hasNextPage, totalCount);
    }

    public static GitLabPage<GitLabMergeRequest> MergeRequestPage(
        IReadOnlyList<GitLabMergeRequest> items,
        bool hasNextPage = false,
        int? totalCount = null)
    {
        return new GitLabPage<GitLabMergeRequest>(items, hasNextPage, totalCount);
    }
}
