using System.Net;
using NSubstitute;
using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps.Tests;

public sealed class AzureDevOpsPullRequestTests
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
                TestData.Project,
                repository.Name,
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest());

        var pullRequest = await provider.CreatePullRequestAsync(repository, request);

        Assert.Equal(7, pullRequest.Number);
        Assert.Equal("Test pull request", pullRequest.Title);
        Assert.Equal("feature", pullRequest.SourceBranch);
        Assert.Equal("main", pullRequest.TargetBranch);
        Assert.Equal(PullRequestState.Open, pullRequest.State);
        Assert.Equal("Test User", pullRequest.Author);
        await client.Received(1).CreatePullRequestAsync(
            TestData.Project,
            repository.Name,
            request,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePullRequest_AsDraft_ReturnsDraftState()
    {
        var (provider, client) = CreateProvider();
        var repository = CreateRepositoryRef();

        client.CreatePullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CreatePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Draft, draft: true));

        var pullRequest = await provider.CreatePullRequestAsync(
            repository,
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
    public async Task CreatePullRequest_WithoutProject_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider(defaultProject: null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.CreatePullRequestAsync(
                new RepositoryRef { Name = "test-repository" },
                new CreatePullRequestRequest
                {
                    Title = "Title",
                    SourceBranch = "feature",
                    TargetBranch = "main",
                }));
    }

    [Fact]
    public async Task CreatePullRequest_WhenUnauthorized_ThrowsAuthenticationException()
    {
        var (provider, client) = CreateProvider();

        client.CreatePullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CreatePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<AzureDevOpsPullRequest>>(_ =>
                throw AzureDevOpsRepositoryTests.CreateResponseException(
                    HttpStatusCode.Unauthorized,
                    "Unauthorized"));

        await Assert.ThrowsAsync<VcsAuthenticationException>(
            () => provider.CreatePullRequestAsync(
                CreateRepositoryRef(),
                new CreatePullRequestRequest
                {
                    Title = "Title",
                    SourceBranch = "feature",
                    TargetBranch = "main",
                }));
    }

    [Fact]
    public async Task GetPullRequest_ReturnsMappedPullRequest()
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync(
                TestData.Project,
                "test-repository",
                7,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest());

        var pullRequest = await provider.GetPullRequestAsync(CreateRepositoryRef(), 7);

        Assert.Equal(7, pullRequest.Number);
        Assert.Equal(
            "https://dev.azure.com/test-organization/test-project/_git/test-repository/pullrequest/7",
            pullRequest.Url);
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
            .Returns<Task<AzureDevOpsPullRequest>>(_ =>
                throw AzureDevOpsRepositoryTests.CreateResponseException(
                    HttpStatusCode.NotFound,
                    "Not Found"));

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
            Author = "Test User",
            SourceBranch = "feature",
            TargetBranch = "main",
        };
        var pagination = new PaginationOptions { Page = 2, PageSize = 5 };

        client.ListPullRequestsAsync(
                TestData.Project,
                "test-repository",
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
        Assert.Equal(5, result.PageSize);
        Assert.True(result.HasNextPage);
        Assert.Equal(9, result.TotalCount);
        await client.Received(1).ListPullRequestsAsync(
            TestData.Project,
            "test-repository",
            query,
            pagination,
            Arg.Any<CancellationToken>());
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
                TestData.PullRequest(number: 1),
                TestData.PullRequest(number: 2, state: PullRequestState.Closed),
                TestData.PullRequest(number: 3, state: PullRequestState.Merged),
            ]));

        var result = await provider.ListPullRequestsAsync(
            CreateRepositoryRef(),
            new PullRequestQuery { State = PullRequestState.Open });

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Number);
    }

    internal static RepositoryRef CreateRepositoryRef()
    {
        return new RepositoryRef
        {
            Owner = TestData.Organization,
            Project = TestData.Project,
            Name = "test-repository",
        };
    }

    internal static (AzureDevOpsProvider Provider, IAzureDevOpsApiClient Client) CreateProvider(
        string? defaultProject = TestData.Project)
    {
        return AzureDevOpsRepositoryTests.CreateProvider(defaultProject);
    }
}
