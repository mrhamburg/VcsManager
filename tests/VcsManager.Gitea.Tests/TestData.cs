using System.Net;
using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

internal static class TestData
{
    public const string Owner = "test-owner";
    public const string RepositoryName = "test-repository";
    public const string BaseUrl = "https://gitea.example.com";

    public static GiteaRepository Repository(
        string owner = Owner,
        string name = RepositoryName,
        VcsVisibility visibility = VcsVisibility.Private,
        bool archived = false)
    {
        return new GiteaRepository
        {
            Id = 42,
            Name = name,
            Owner = owner,
            FullName = $"{owner}/{name}",
            WebUrl = $"{BaseUrl}/{owner}/{name}",
            DefaultBranch = "main",
            Visibility = visibility,
            Description = "Test repository",
            IsArchived = archived,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };
    }

    public static GiteaPullRequest PullRequest(
        int number = 7,
        PullRequestState state = PullRequestState.Open,
        bool draft = false,
        string author = "test-user",
        string sourceBranch = "feature",
        string targetBranch = "main")
    {
        return new GiteaPullRequest
        {
            Id = 100 + number,
            Number = number,
            Title = "Test pull request",
            Description = "A test pull request",
            SourceBranch = sourceBranch,
            TargetBranch = targetBranch,
            State = state,
            IsDraft = draft,
            Author = author,
            WebUrl = $"{BaseUrl}/{Owner}/{RepositoryName}/pulls/{number}",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            MergedAt = state == PullRequestState.Merged ? DateTimeOffset.UnixEpoch : null,
            ClosedAt = state == PullRequestState.Closed ? DateTimeOffset.UnixEpoch : null,
        };
    }

    public static GiteaPage<GiteaRepository> RepositoryPage(
        IReadOnlyList<GiteaRepository> items,
        bool hasNextPage = false,
        int? totalCount = null)
    {
        return new GiteaPage<GiteaRepository>(items, hasNextPage, totalCount);
    }

    public static GiteaPage<GiteaPullRequest> PullRequestPage(
        IReadOnlyList<GiteaPullRequest> items,
        bool hasNextPage = false,
        int? totalCount = null)
    {
        return new GiteaPage<GiteaPullRequest>(items, hasNextPage, totalCount);
    }

    public static GiteaApiException ApiException(
        HttpStatusCode statusCode,
        string body = "{}",
        TimeSpan? retryAfter = null)
    {
        return new GiteaApiException(statusCode, body, retryAfter);
    }
}
