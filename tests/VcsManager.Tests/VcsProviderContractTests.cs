namespace VcsManager.Tests;

public class VcsProviderContractTests
{
    private static readonly RepositoryRef Repository = new() { Owner = "acme", Name = "core" };

    [Fact]
    public void FakeProvider_ImplementsContract()
    {
        IVcsProvider provider = new FakeVcsProvider();

        Assert.IsAssignableFrom<IAsyncDisposable>(provider);
    }

    [Fact]
    public async Task ListRepositories_AcceptsNullQueryAndPagination()
    {
        await using var provider = new FakeVcsProvider();

        var result = await provider.ListRepositoriesAsync(null, null);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(30, result.PageSize);
    }

    [Fact]
    public async Task RepositoryLifecycle_RoundTrips()
    {
        await using var provider = new FakeVcsProvider();
        var request = new CreateRepositoryRequest
        {
            Name = Repository.Name,
            Owner = Repository.Owner,
            DefaultBranch = "main",
        };

        var created = await provider.CreateRepositoryAsync(request);
        var fetched = await provider.GetRepositoryAsync(Repository);

        Assert.Equal(created, fetched);
        Assert.True(await provider.RepositoryExistsAsync(Repository));

        await provider.DeleteRepositoryAsync(Repository);

        Assert.False(await provider.RepositoryExistsAsync(Repository));
    }

    [Fact]
    public async Task GetRepository_WhenMissing_ThrowsRepositoryNotFoundException()
    {
        await using var provider = new FakeVcsProvider();

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.GetRepositoryAsync(new RepositoryRef { Name = "missing" }));
    }

    [Fact]
    public async Task CreatePullRequest_RoundTrips()
    {
        await using var provider = await CreateProviderWithRepositoryAsync();
        var request = new CreatePullRequestRequest
        {
            Title = "Add feature",
            SourceBranch = "feature",
            TargetBranch = "main",
        };

        var created = await provider.CreatePullRequestAsync(Repository, request);
        var fetched = await provider.GetPullRequestAsync(Repository, created.Number);

        Assert.Equal(created, fetched);
        Assert.Equal(PullRequestState.Open, created.State);
    }

    [Fact]
    public async Task ListPullRequests_ReportsPageMetadataAndFilters()
    {
        await using var provider = await CreateProviderWithRepositoryAsync();
        await provider.CreatePullRequestAsync(
            Repository,
            new CreatePullRequestRequest { Title = "One", SourceBranch = "one", TargetBranch = "main" });
        await provider.CreatePullRequestAsync(
            Repository,
            new CreatePullRequestRequest
            {
                Title = "Two",
                SourceBranch = "two",
                TargetBranch = "main",
                Draft = true,
            });
        await provider.CreatePullRequestAsync(
            Repository,
            new CreatePullRequestRequest { Title = "Three", SourceBranch = "three", TargetBranch = "main" });

        var page = await provider.ListPullRequestsAsync(
            Repository,
            pagination: new PaginationOptions { PageSize = 2 });

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.TotalCount);
        Assert.True(page.HasNextPage);

        var drafts = await provider.ListPullRequestsAsync(
            Repository,
            new PullRequestQuery { State = PullRequestState.Draft });

        var draft = Assert.Single(drafts.Items);
        Assert.Equal("Two", draft.Title);
    }

    [Fact]
    public async Task MergePullRequest_AcceptsNullRequest()
    {
        await using var provider = await CreateProviderWithRepositoryAsync();
        var created = await CreatePullRequestAsync(provider);

        var merged = await provider.MergePullRequestAsync(Repository, created.Number, null);

        Assert.Equal(PullRequestState.Merged, merged.State);
        Assert.NotNull(merged.MergedAt);
    }

    [Fact]
    public async Task ClosePullRequest_IsIdempotent()
    {
        await using var provider = await CreateProviderWithRepositoryAsync();
        var created = await CreatePullRequestAsync(provider);

        var first = await provider.ClosePullRequestAsync(Repository, created.Number);
        var second = await provider.ClosePullRequestAsync(Repository, created.Number);

        Assert.Equal(PullRequestState.Closed, first.State);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task ClosePullRequest_WhenMerged_ReturnsPullRequestUnchanged()
    {
        await using var provider = await CreateProviderWithRepositoryAsync();
        var created = await CreatePullRequestAsync(provider);
        await provider.MergePullRequestAsync(Repository, created.Number);

        var closed = await provider.ClosePullRequestAsync(Repository, created.Number);

        Assert.Equal(PullRequestState.Merged, closed.State);
    }

    [Fact]
    public async Task DisposeAsync_MarksProviderDisposed()
    {
        var provider = new FakeVcsProvider();

        await provider.DisposeAsync();

        Assert.True(provider.IsDisposed);
    }

    private static async Task<FakeVcsProvider> CreateProviderWithRepositoryAsync()
    {
        var provider = new FakeVcsProvider();
        await provider.CreateRepositoryAsync(new CreateRepositoryRequest
        {
            Name = Repository.Name,
            Owner = Repository.Owner,
            DefaultBranch = "main",
        });

        return provider;
    }

    private static Task<PullRequest> CreatePullRequestAsync(FakeVcsProvider provider)
    {
        return provider.CreatePullRequestAsync(
            Repository,
            new CreatePullRequestRequest
            {
                Title = "Add feature",
                SourceBranch = "feature",
                TargetBranch = "main",
            });
    }
}
