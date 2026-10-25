using System.Net;
using NSubstitute;
using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaPullRequestTests
{
    [Fact]
    public async Task CreatePullRequest_PassesRequestAndMapsResult()
    {
        var (provider, client) = CreateProvider();
        var repository = CreateRepositoryRef();
        var request = new CreatePullRequestRequest
        {
            Title = "Test pull request",
            Description = "A test pull request",
            SourceBranch = "feature",
            TargetBranch = "main",
        };

        client.CreatePullRequestAsync(
                TestData.Owner,
                TestData.RepositoryName,
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest());

        var pullRequest = await provider.CreatePullRequestAsync(repository, request);

        Assert.Equal(7, pullRequest.Number);
        Assert.Equal("feature", pullRequest.SourceBranch);
        Assert.Equal("main", pullRequest.TargetBranch);
        Assert.Equal(PullRequestState.Open, pullRequest.State);
        await client.Received(1).CreatePullRequestAsync(
            TestData.Owner,
            TestData.RepositoryName,
            request,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePullRequest_AsDraft_ReturnsDraftState()
    {
        var (provider, client) = CreateProvider();

        client.CreatePullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CreatePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Draft, draft: true));

        var pullRequest = await provider.CreatePullRequestAsync(
            CreateRepositoryRef(),
            new CreatePullRequestRequest
            {
                Title = "Draft pull request",
                SourceBranch = "feature",
                TargetBranch = "main",
                Draft = true,
            });

        Assert.True(pullRequest.IsDraft);
        Assert.Equal(PullRequestState.Draft, pullRequest.State);
    }

    [Theory]
    [InlineData("", "feature", "main")]
    [InlineData("Title", "", "main")]
    [InlineData("Title", "feature", "")]
    public async Task CreatePullRequest_WithMissingField_ThrowsArgumentException(
        string title,
        string sourceBranch,
        string targetBranch)
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.CreatePullRequestAsync(
                CreateRepositoryRef(),
                new CreatePullRequestRequest
                {
                    Title = title,
                    SourceBranch = sourceBranch,
                    TargetBranch = targetBranch,
                }));
    }

    [Fact]
    public async Task GetPullRequest_ReturnsMappedPullRequest()
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync(
                TestData.Owner,
                TestData.RepositoryName,
                7,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest());

        var pullRequest = await provider.GetPullRequestAsync(CreateRepositoryRef(), 7);

        Assert.Equal(7, pullRequest.Number);
        Assert.Equal("https://gitea.example.com/test-owner/test-repository/pulls/7", pullRequest.Url);
    }

    [Fact]
    public async Task GetPullRequest_Missing_ThrowsPullRequestNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GiteaPullRequest>>(_ => throw TestData.ApiException(
                HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<PullRequestNotFoundException>(
            () => provider.GetPullRequestAsync(CreateRepositoryRef(), 7));
    }

    [Fact]
    public async Task GetPullRequest_WithInvalidNumber_ThrowsArgumentOutOfRangeException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => provider.GetPullRequestAsync(CreateRepositoryRef(), 0));
    }

    [Fact]
    public async Task ListPullRequests_PassesQueryAndPagination()
    {
        var (provider, client) = CreateProvider();
        var query = new PullRequestQuery
        {
            State = PullRequestState.Open,
            Author = "test-user",
            SourceBranch = "feature",
            TargetBranch = "main",
        };
        var pagination = new PaginationOptions { Page = 2, PageSize = 100 };

        client.ListPullRequestsAsync(
                TestData.Owner,
                TestData.RepositoryName,
                query,
                pagination,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequestPage(
                [TestData.PullRequest()],
                hasNextPage: true,
                totalCount: 9));

        var result = await provider.ListPullRequestsAsync(
            CreateRepositoryRef(),
            query,
            pagination);

        Assert.Single(result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.True(result.HasNextPage);
        Assert.Equal(9, result.TotalCount);
    }

    [Fact]
    public async Task ListPullRequests_FiltersResultsOnThePage()
    {
        var (provider, client) = CreateProvider();

        client.ListPullRequestsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<PullRequestQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequestPage(
            [
                TestData.PullRequest(number: 1, state: PullRequestState.Open, author: "alice"),
                TestData.PullRequest(number: 2, state: PullRequestState.Closed, author: "alice"),
                TestData.PullRequest(number: 3, state: PullRequestState.Open, author: "bob"),
                TestData.PullRequest(number: 4, state: PullRequestState.Open, sourceBranch: "other"),
            ]));

        var result = await provider.ListPullRequestsAsync(
            CreateRepositoryRef(),
            new PullRequestQuery
            {
                State = PullRequestState.Open,
                Author = "alice",
                SourceBranch = "feature",
            });

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Number);
    }

    [Fact]
    public async Task ListPullRequests_WhenClientThrows_MapsProviderException()
    {
        var (provider, client) = CreateProvider();

        client.ListPullRequestsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<PullRequestQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GiteaPage<GiteaPullRequest>>>(_ =>
                throw TestData.ApiException(HttpStatusCode.Forbidden));

        await Assert.ThrowsAsync<VcsAuthorizationException>(
            () => provider.ListPullRequestsAsync(CreateRepositoryRef()));
    }

    internal static RepositoryRef CreateRepositoryRef()
    {
        return new RepositoryRef
        {
            Owner = TestData.Owner,
            Name = TestData.RepositoryName,
        };
    }

    internal static (GiteaProvider Provider, IGiteaApiClient Client) CreateProvider(
        string? defaultOwner = TestData.Owner)
    {
        return GiteaRepositoryTests.CreateProvider(defaultOwner);
    }
}
