using System.Net;
using NSubstitute;
using VcsManager.GitLab.Internal;

namespace VcsManager.GitLab.Tests;

public sealed class GitLabPullRequestTests
{
    [Fact]
    public async Task CreatePullRequest_MapsFieldsAndDraftState()
    {
        var (provider, client) = CreateProvider();
        var repository = Repository();
        var request = new CreatePullRequestRequest
        {
            Title = "Test merge request",
            Description = "A test merge request",
            SourceBranch = "feature",
            TargetBranch = "main",
            Draft = true,
        };

        client.CreateMergeRequestAsync(
                "test-group/test-project",
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.MergeRequest(state: PullRequestState.Draft));

        var pullRequest = await provider.CreatePullRequestAsync(repository, request);

        Assert.Equal(7, pullRequest.Number);
        Assert.Equal("Test merge request", pullRequest.Title);
        Assert.Equal("feature", pullRequest.SourceBranch);
        Assert.Equal("main", pullRequest.TargetBranch);
        Assert.Equal(PullRequestState.Draft, pullRequest.State);
        Assert.True(pullRequest.IsDraft);
    }

    [Theory]
    [InlineData(PullRequestState.Open)]
    [InlineData(PullRequestState.Draft)]
    [InlineData(PullRequestState.Closed)]
    [InlineData(PullRequestState.Merged)]
    public async Task CreatePullRequest_MapsEveryState(PullRequestState state)
    {
        var (provider, client) = CreateProvider();
        var request = CreatePullRequestRequest();

        client.CreateMergeRequestAsync(
                "test-group/test-project",
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.MergeRequest(state: state));

        var pullRequest = await provider.CreatePullRequestAsync(Repository(), request);

        Assert.Equal(state, pullRequest.State);
        Assert.Equal(state == PullRequestState.Draft, pullRequest.IsDraft);
    }

    [Fact]
    public async Task CreatePullRequest_WithInvalidBranch_ThrowsVcsConflictException()
    {
        var (provider, client) = CreateProvider();

        client.CreateMergeRequestAsync(
                "test-group/test-project",
                Arg.Any<CreatePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GitLabMergeRequest>>(_ => throw new GitLabApiException(
                HttpStatusCode.UnprocessableEntity,
                "{\"message\":[\"Source branch does not exist\"]}",
                retryAfter: null));

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.CreatePullRequestAsync(Repository(), CreatePullRequestRequest()));
    }

    [Fact]
    public async Task CreatePullRequest_WithMissingRepository_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.CreateMergeRequestAsync(
                "test-group/test-project",
                Arg.Any<CreatePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GitLabMergeRequest>>(_ => throw new GitLabApiException(
                HttpStatusCode.NotFound,
                "Not Found",
                retryAfter: null));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.CreatePullRequestAsync(Repository(), CreatePullRequestRequest()));
    }

    [Theory]
    [InlineData("", "feature", "main")]
    [InlineData("Test", "", "main")]
    [InlineData("Test", "feature", "")]
    public async Task CreatePullRequest_WithMissingRequiredValue_ThrowsArgumentException(
        string title,
        string sourceBranch,
        string targetBranch)
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.CreatePullRequestAsync(
            Repository(),
            new CreatePullRequestRequest
            {
                Title = title,
                SourceBranch = sourceBranch,
                TargetBranch = targetBranch,
            }));
    }

    [Fact]
    public async Task GetPullRequest_Missing_ThrowsPullRequestNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.GetMergeRequestAsync(
                "test-group/test-project",
                7,
                Arg.Any<CancellationToken>())
            .Returns<Task<GitLabMergeRequest>>(_ => throw new GitLabApiException(
                HttpStatusCode.NotFound,
                "Not Found",
                retryAfter: null));

        await Assert.ThrowsAsync<PullRequestNotFoundException>(
            () => provider.GetPullRequestAsync(Repository(), 7));
    }

    [Fact]
    public async Task GetPullRequest_WithInvalidNumber_ThrowsArgumentOutOfRangeException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => provider.GetPullRequestAsync(Repository(), 0));
    }

    [Fact]
    public async Task ListPullRequests_ForwardsFiltersAndPagination()
    {
        var (provider, client) = CreateProvider();
        var query = new PullRequestQuery
        {
            State = PullRequestState.Open,
            Author = "test-user",
            SourceBranch = "feature",
            TargetBranch = "main",
        };
        var pagination = new PaginationOptions { Page = 2, PageSize = 10 };

        client.ListMergeRequestsAsync(
                "test-group/test-project",
                query,
                pagination,
                Arg.Any<CancellationToken>())
            .Returns(TestData.MergeRequestPage(
                [TestData.MergeRequest()],
                hasNextPage: true,
                totalCount: 3));

        var result = await provider.ListPullRequestsAsync(Repository(), query, pagination);

        Assert.Single(result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.True(result.HasNextPage);
        Assert.Equal(3, result.TotalCount);
        await client.Received(1).ListMergeRequestsAsync(
            "test-group/test-project",
            query,
            pagination,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListPullRequests_AppliesStateAuthorSourceAndTargetFilters()
    {
        var (provider, client) = CreateProvider();
        var query = new PullRequestQuery
        {
            State = PullRequestState.Open,
            Author = "test-user",
            SourceBranch = "feature",
            TargetBranch = "main",
        };

        var open = TestData.MergeRequest(iid: 1);
        client.ListMergeRequestsAsync(
                "test-group/test-project",
                query,
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.MergeRequestPage(
            [
                open,
                TestData.MergeRequest(iid: 2, state: PullRequestState.Draft),
                TestData.MergeRequest(iid: 3, state: PullRequestState.Closed),
                TestData.MergeRequest(iid: 4, state: PullRequestState.Merged),
                open with { Iid = 5, Id = 105, Author = "other-user" },
                open with { Iid = 6, Id = 106, SourceBranch = "other" },
                open with { Iid = 7, Id = 107, TargetBranch = "other" },
            ]));

        var result = await provider.ListPullRequestsAsync(Repository(), query);

        var pullRequest = Assert.Single(result.Items);
        Assert.Equal(1, pullRequest.Number);
    }

    [Fact]
    public async Task ListPullRequests_ForwardsPageAndSize()
    {
        var (provider, client) = CreateProvider();
        var pagination = new PaginationOptions { Page = 3, PageSize = 25 };

        client.ListMergeRequestsAsync(
                Arg.Any<string>(),
                Arg.Any<PullRequestQuery>(),
                pagination,
                Arg.Any<CancellationToken>())
            .Returns(TestData.MergeRequestPage([]));

        await provider.ListPullRequestsAsync(Repository(), pagination: pagination);

        await client.Received(1).ListMergeRequestsAsync(
            "test-group/test-project",
            Arg.Any<PullRequestQuery>(),
            pagination,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListPullRequests_MissingRepository_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.ListMergeRequestsAsync(
                Arg.Any<string>(),
                Arg.Any<PullRequestQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GitLabPage<GitLabMergeRequest>>>(_ => throw new GitLabApiException(
                HttpStatusCode.NotFound,
                "Not Found",
                retryAfter: null));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.ListPullRequestsAsync(Repository()));
    }

    private static RepositoryRef Repository()
    {
        return new RepositoryRef { Owner = "test-group", Name = "test-project" };
    }

    private static CreatePullRequestRequest CreatePullRequestRequest()
    {
        return new CreatePullRequestRequest
        {
            Title = "Test merge request",
            SourceBranch = "feature",
            TargetBranch = "main",
        };
    }

    private static (GitLabProvider Provider, IGitLabApiClient Client) CreateProvider(
        GitLabOptions? options = null)
    {
        var client = Substitute.For<IGitLabApiClient>();
        return (
            new GitLabProvider(client, options ?? new GitLabOptions { Token = "test-token" }),
            client);
    }
}
