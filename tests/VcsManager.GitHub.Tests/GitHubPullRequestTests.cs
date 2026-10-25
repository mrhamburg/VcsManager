using System.Net;
using NSubstitute;
using Octokit;
using VcsManager.GitHub.Internal;

namespace VcsManager.GitHub.Tests;

public sealed class GitHubPullRequestTests
{
    [Fact]
    public async Task CreatePullRequest_MapsFieldsAndDraftState()
    {
        var (provider, client) = CreateProvider();
        var repository = Repository();
        var request = new CreatePullRequestRequest
        {
            Title = "Test pull request",
            Description = "A test pull request",
            SourceBranch = "feature",
            TargetBranch = "main",
            Draft = true,
        };

        client.CreatePullRequestAsync(
                "test-owner",
                "test-repo",
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Draft));

        var pullRequest = await provider.CreatePullRequestAsync(repository, request);

        Assert.Equal(7, pullRequest.Number);
        Assert.Equal("Test pull request", pullRequest.Title);
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

        client.CreatePullRequestAsync(
                "test-owner",
                "test-repo",
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: state));

        var pullRequest = await provider.CreatePullRequestAsync(Repository(), request);

        Assert.Equal(state, pullRequest.State);
        Assert.Equal(state == PullRequestState.Draft, pullRequest.IsDraft);
    }

    [Fact]
    public async Task CreatePullRequest_WithInvalidBranch_ThrowsVcsConflictException()
    {
        var (provider, client) = CreateProvider();

        client.CreatePullRequestAsync(
                "test-owner",
                "test-repo",
                Arg.Any<CreatePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GitHubPullRequest>>(_ => throw new ApiException(
                "Validation Failed",
                HttpStatusCode.UnprocessableEntity));

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.CreatePullRequestAsync(Repository(), CreatePullRequestRequest()));
    }

    [Fact]
    public async Task CreatePullRequest_WithMissingRepository_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.CreatePullRequestAsync(
                "test-owner",
                "test-repo",
                Arg.Any<CreatePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GitHubPullRequest>>(_ => throw new ApiException(
                "Not Found",
                HttpStatusCode.NotFound));

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

        client.GetPullRequestAsync("test-owner", "test-repo", 7, Arg.Any<CancellationToken>())
            .Returns<Task<GitHubPullRequest>>(_ => throw new ApiException(
                "Not Found",
                HttpStatusCode.NotFound));

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

        client.ListPullRequestsAsync(
                "test-owner",
                "test-repo",
                query,
                pagination,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequestPage([TestData.PullRequest()], hasNextPage: true));

        var result = await provider.ListPullRequestsAsync(Repository(), query, pagination);

        Assert.Single(result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.True(result.HasNextPage);
        Assert.Null(result.TotalCount);
        await client.Received(1).ListPullRequestsAsync(
            "test-owner",
            "test-repo",
            query,
            pagination,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergePullRequest_PassesMergeMethodAndCommitDetails()
    {
        var client = new FakeGitHubApiClient();
        await using var provider = CreateProvider(client);
        var repository = Repository();
        await provider.CreateRepositoryAsync(CreateRepositoryRequest());
        var pullRequest = await provider.CreatePullRequestAsync(repository, CreatePullRequestRequest());
        var request = new MergePullRequestRequest
        {
            Method = MergeMethod.Squash,
            CommitTitle = "Squash title",
            CommitMessage = "Squash message",
        };

        var merged = await provider.MergePullRequestAsync(repository, pullRequest.Number, request);

        Assert.Equal(PullRequestState.Merged, merged.State);
        Assert.NotNull(merged.MergedAt);
        Assert.Equal(MergeMethod.Squash, client.LastMergeRequest?.Method);
        Assert.Equal("Squash title", client.LastMergeRequest?.CommitTitle);
        Assert.Equal("Squash message", client.LastMergeRequest?.CommitMessage);
    }

    [Theory]
    [InlineData(MergeMethod.Merge)]
    [InlineData(MergeMethod.Squash)]
    [InlineData(MergeMethod.Rebase)]
    public async Task MergePullRequest_SupportsEveryMergeMethod(MergeMethod method)
    {
        var client = new FakeGitHubApiClient();
        await using var provider = CreateProvider(client);
        var repository = Repository();
        await provider.CreateRepositoryAsync(CreateRepositoryRequest());
        var pullRequest = await provider.CreatePullRequestAsync(repository, CreatePullRequestRequest());

        var merged = await provider.MergePullRequestAsync(
            repository,
            pullRequest.Number,
            new MergePullRequestRequest { Method = method });

        Assert.Equal(PullRequestState.Merged, merged.State);
        Assert.Equal(method, client.LastMergeRequest?.Method);
    }

    [Fact]
    public async Task MergePullRequest_DeleteSourceBranch_DeletesAfterSuccess()
    {
        var client = new FakeGitHubApiClient();
        await using var provider = CreateProvider(client);
        var repository = Repository();
        await provider.CreateRepositoryAsync(CreateRepositoryRequest());
        var pullRequest = await provider.CreatePullRequestAsync(repository, CreatePullRequestRequest());

        await provider.MergePullRequestAsync(
            repository,
            pullRequest.Number,
            new MergePullRequestRequest { DeleteSourceBranch = true });

        Assert.Equal(new[] { "feature" }, client.DeletedBranches);
    }

    [Fact]
    public async Task MergePullRequest_ForkSource_DoesNotDeleteBranch()
    {
        var client = new FakeGitHubApiClient { SourceBranchIsFromFork = true };
        await using var provider = CreateProvider(client);
        var repository = Repository();
        await provider.CreateRepositoryAsync(CreateRepositoryRequest());
        var pullRequest = await provider.CreatePullRequestAsync(repository, CreatePullRequestRequest());

        await provider.MergePullRequestAsync(
            repository,
            pullRequest.Number,
            new MergePullRequestRequest { DeleteSourceBranch = true });

        Assert.Empty(client.DeletedBranches);
    }

    [Fact]
    public async Task MergePullRequest_Conflict_ThrowsVcsConflictException()
    {
        var client = new FakeGitHubApiClient { MergeConflict = true };
        await using var provider = CreateProvider(client);
        var repository = Repository();
        await provider.CreateRepositoryAsync(CreateRepositoryRequest());
        var pullRequest = await provider.CreatePullRequestAsync(repository, CreatePullRequestRequest());

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.MergePullRequestAsync(repository, pullRequest.Number));

        Assert.Empty(client.DeletedBranches);
    }

    [Fact]
    public async Task MergePullRequest_UnsupportedMethod_ThrowsVcsUnsupportedOperationException()
    {
        var client = new FakeGitHubApiClient { UnsupportedMergeMethod = MergeMethod.Rebase };
        await using var provider = CreateProvider(client);
        var repository = Repository();
        await provider.CreateRepositoryAsync(CreateRepositoryRequest());
        var pullRequest = await provider.CreatePullRequestAsync(repository, CreatePullRequestRequest());

        await Assert.ThrowsAsync<VcsUnsupportedOperationException>(
            () => provider.MergePullRequestAsync(
                repository,
                pullRequest.Number,
                new MergePullRequestRequest { Method = MergeMethod.Rebase }));
    }

    [Fact]
    public async Task MergePullRequest_NotFound_ThrowsPullRequestNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.MergePullRequestAsync(
                "test-owner",
                "test-repo",
                Arg.Any<int>(),
                Arg.Any<MergePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GitHubPullRequest>>(_ => throw new ApiException(
                "Not Found",
                HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<PullRequestNotFoundException>(
            () => provider.MergePullRequestAsync(Repository(), 7));
    }

    [Fact]
    public async Task MergePullRequest_BranchDeletionFailure_IsNotHidden()
    {
        var client = new FakeGitHubApiClient
        {
            DeleteBranchFailure = new VcsAuthorizationException("Branch is protected."),
        };
        await using var provider = CreateProvider(client);
        var repository = Repository();
        await provider.CreateRepositoryAsync(CreateRepositoryRequest());
        var pullRequest = await provider.CreatePullRequestAsync(repository, CreatePullRequestRequest());

        await Assert.ThrowsAsync<VcsAuthorizationException>(
            () => provider.MergePullRequestAsync(
                repository,
                pullRequest.Number,
                new MergePullRequestRequest { DeleteSourceBranch = true }));
    }

    [Fact]
    public async Task ClosePullRequest_Open_ReturnsClosed()
    {
        var client = new FakeGitHubApiClient();
        await using var provider = CreateProvider(client);
        var repository = Repository();
        await provider.CreateRepositoryAsync(CreateRepositoryRequest());
        var pullRequest = await provider.CreatePullRequestAsync(repository, CreatePullRequestRequest());

        var closed = await provider.ClosePullRequestAsync(repository, pullRequest.Number);

        Assert.Equal(PullRequestState.Closed, closed.State);
        Assert.NotNull(closed.ClosedAt);
        Assert.Equal(1, client.CloseCallCount);
    }

    [Fact]
    public async Task ClosePullRequest_IsIdempotent()
    {
        var client = new FakeGitHubApiClient();
        await using var provider = CreateProvider(client);
        var repository = Repository();
        await provider.CreateRepositoryAsync(CreateRepositoryRequest());
        var pullRequest = await provider.CreatePullRequestAsync(repository, CreatePullRequestRequest());

        var first = await provider.ClosePullRequestAsync(repository, pullRequest.Number);
        var second = await provider.ClosePullRequestAsync(repository, pullRequest.Number);

        Assert.Equal(PullRequestState.Closed, first.State);
        Assert.Equal(PullRequestState.Closed, second.State);
        Assert.Equal(1, client.CloseCallCount);
    }

    [Fact]
    public async Task ClosePullRequest_Merged_ReturnsMergedState()
    {
        var client = new FakeGitHubApiClient();
        await using var provider = CreateProvider(client);
        var repository = Repository();
        await provider.CreateRepositoryAsync(CreateRepositoryRequest());
        var pullRequest = await provider.CreatePullRequestAsync(repository, CreatePullRequestRequest());
        await provider.MergePullRequestAsync(repository, pullRequest.Number);

        var closed = await provider.ClosePullRequestAsync(repository, pullRequest.Number);

        Assert.Equal(PullRequestState.Merged, closed.State);
        Assert.Equal(0, client.CloseCallCount);
    }

    [Fact]
    public async Task ClosePullRequest_NotFound_ThrowsPullRequestNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync("test-owner", "test-repo", 7, Arg.Any<CancellationToken>())
            .Returns<Task<GitHubPullRequest>>(_ => throw new ApiException(
                "Not Found",
                HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<PullRequestNotFoundException>(
            () => provider.ClosePullRequestAsync(Repository(), 7));
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

        var open = TestData.PullRequest(number: 1);
        client.ListPullRequestsAsync(
                "test-owner",
                "test-repo",
                query,
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequestPage(
            [
                open,
                TestData.PullRequest(number: 2, state: PullRequestState.Draft),
                TestData.PullRequest(number: 3, state: PullRequestState.Closed),
                TestData.PullRequest(number: 4, state: PullRequestState.Merged),
                open with { Number = 5, Id = 105, Author = "other-user" },
                open with { Number = 6, Id = 106, SourceBranch = "other" },
                open with { Number = 7, Id = 107, TargetBranch = "other" },
            ]));

        var result = await provider.ListPullRequestsAsync(Repository(), query);

        var pullRequest = Assert.Single(result.Items);
        Assert.Equal(1, pullRequest.Number);
    }

    [Fact]
    public async Task ListPullRequests_PaddedAuthor_IsNormalized()
    {
        var (provider, client) = CreateProvider();

        client.ListPullRequestsAsync(
                "test-owner",
                "test-repo",
                Arg.Any<PullRequestQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequestPage(
            [
                TestData.PullRequest(),
                TestData.PullRequest(number: 8) with { Id = 108, Author = "other-user" },
            ]));

        var result = await provider.ListPullRequestsAsync(
            Repository(),
            new PullRequestQuery { Author = " test-user " });

        var pullRequest = Assert.Single(result.Items);
        Assert.Equal("test-user", pullRequest.Author);
        await client.Received(1).ListPullRequestsAsync(
            "test-owner",
            "test-repo",
            Arg.Is<PullRequestQuery>(query => query.Author == "test-user"),
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListPullRequests_MissingRepository_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.ListPullRequestsAsync(
                "test-owner",
                "test-repo",
                Arg.Any<PullRequestQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GitHubPage<GitHubPullRequest>>>(_ => throw new ApiException(
                "Not Found",
                HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.ListPullRequestsAsync(Repository()));
    }

    [Fact]
    public async Task DeleteRepository_Succeeds()
    {
        var client = new FakeGitHubApiClient();
        await using var provider = CreateProvider(client);
        var repository = Repository();
        await provider.CreateRepositoryAsync(CreateRepositoryRequest());

        await provider.DeleteRepositoryAsync(repository);

        Assert.False(await provider.RepositoryExistsAsync(repository));
    }

    [Fact]
    public async Task GetPullRequest_MapsFields()
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync("test-owner", "test-repo", 7, Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest());

        var pullRequest = await provider.GetPullRequestAsync(Repository(), 7);

        Assert.Equal(7, pullRequest.Number);
        Assert.Equal("Test pull request", pullRequest.Title);
        Assert.Equal("feature", pullRequest.SourceBranch);
        Assert.Equal("main", pullRequest.TargetBranch);
        Assert.Equal("test-user", pullRequest.Author);
        Assert.Equal(PullRequestState.Open, pullRequest.State);
    }

    private static RepositoryRef Repository()
    {
        return new RepositoryRef { Owner = "test-owner", Name = "test-repo" };
    }

    private static CreateRepositoryRequest CreateRepositoryRequest()
    {
        return new CreateRepositoryRequest { Owner = "test-owner", Name = "test-repo" };
    }

    private static CreatePullRequestRequest CreatePullRequestRequest()
    {
        return new CreatePullRequestRequest
        {
            Title = "Test pull request",
            SourceBranch = "feature",
            TargetBranch = "main",
        };
    }

    private static GitHubProvider CreateProvider(FakeGitHubApiClient client)
    {
        return new GitHubProvider(client, new GitHubOptions { Token = "test-token" });
    }

    private static (GitHubProvider Provider, IGitHubApiClient Client) CreateProvider()
    {
        var client = Substitute.For<IGitHubApiClient>();
        return (new GitHubProvider(client, new GitHubOptions { Token = "test-token" }), client);
    }
}
