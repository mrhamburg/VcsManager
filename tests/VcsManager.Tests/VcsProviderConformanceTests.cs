namespace VcsManager.Tests;

/// <summary>
/// Shared behavior that every provider must satisfy. Provider test projects
/// inherit this class and supply factories that use fake provider clients.
/// </summary>
public abstract class VcsProviderConformanceTests
{
    /// <summary>
    /// Gets the merge method that the test provider cannot execute.
    /// </summary>
    protected virtual MergeMethod UnsupportedMergeMethod => MergeMethod.Rebase;

    /// <summary>
    /// Creates a provider backed by a fake client.
    /// </summary>
    protected abstract IVcsProvider CreateProvider();

    /// <summary>
    /// Creates a provider whose fake client reports a merge conflict.
    /// </summary>
    protected abstract IVcsProvider CreateProviderWithMergeConflict();

    /// <summary>
    /// Creates a provider whose fake client rejects <see cref="UnsupportedMergeMethod"/>.
    /// </summary>
    protected abstract IVcsProvider CreateProviderWithUnsupportedMergeMethod();

    [Fact]
    public async Task RepositoryLifecycle_RoundTrips()
    {
        await using var provider = CreateProvider();
        var repository = CreateRepositoryRef("lifecycle");
        var request = CreateRepositoryRequest("lifecycle");

        var created = await provider.CreateRepositoryAsync(request);

        Assert.Equal(repository.Name, created.Name);
        Assert.True(await provider.RepositoryExistsAsync(repository));

        var fetched = await provider.GetRepositoryAsync(repository);
        Assert.Equal(created.FullName, fetched.FullName);

        var listed = await provider.ListRepositoriesAsync(
            new RepositoryQuery { NameContains = repository.Name },
            new PaginationOptions { PageSize = 10 });
        Assert.Contains(listed.Items, item => item.Name == repository.Name);

        await provider.DeleteRepositoryAsync(repository);

        Assert.False(await provider.RepositoryExistsAsync(repository));
        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.GetRepositoryAsync(repository));
    }

    [Fact]
    public async Task ListRepositories_ReportsPaginationMetadata()
    {
        await using var provider = CreateProvider();
        const string prefix = "pagination";

        for (var index = 1; index <= 3; index++)
        {
            await provider.CreateRepositoryAsync(CreateRepositoryRequest($"{prefix}-{index}"));
        }

        var first = await provider.ListRepositoriesAsync(
            new RepositoryQuery { NameContains = prefix },
            new PaginationOptions { Page = 1, PageSize = 2 });

        Assert.Equal(2, first.Items.Count);
        Assert.Equal(1, first.Page);
        Assert.Equal(2, first.PageSize);
        Assert.True(first.HasNextPage);
        Assert.True(first.TotalCount is null || first.TotalCount >= 3);

        var second = await provider.ListRepositoriesAsync(
            new RepositoryQuery { NameContains = prefix },
            new PaginationOptions { Page = 2, PageSize = 2 });

        Assert.NotEmpty(second.Items);
        Assert.False(second.HasNextPage);
        Assert.All(second.Items, item => Assert.StartsWith(prefix, item.Name));
    }

    [Fact]
    public async Task PullRequestLifecycle_RoundTrips()
    {
        await using var provider = CreateProvider();
        var repository = CreateRepositoryRef("pull-requests");
        await provider.CreateRepositoryAsync(CreateRepositoryRequest(repository.Name));
        var request = CreatePullRequestRequest("feature");

        var created = await provider.CreatePullRequestAsync(repository, request);

        Assert.True(created.Number > 0);
        Assert.Equal(PullRequestState.Open, created.State);

        var fetched = await provider.GetPullRequestAsync(repository, created.Number);
        Assert.Equal(created.Number, fetched.Number);

        var listed = await provider.ListPullRequestsAsync(
            repository,
            new PullRequestQuery { SourceBranch = request.SourceBranch });
        Assert.Contains(listed.Items, pullRequest => pullRequest.Number == created.Number);

        var merged = await provider.MergePullRequestAsync(repository, created.Number);

        Assert.Equal(PullRequestState.Merged, merged.State);
        Assert.NotNull(merged.MergedAt);
    }

    [Fact]
    public async Task ClosePullRequest_IsIdempotent()
    {
        await using var provider = CreateProvider();
        var (repository, pullRequest) = await SeedRepositoryWithPullRequestAsync(
            provider,
            "close-idempotent");

        var first = await provider.ClosePullRequestAsync(repository, pullRequest.Number);
        var second = await provider.ClosePullRequestAsync(repository, pullRequest.Number);

        Assert.Equal(PullRequestState.Closed, first.State);
        Assert.Equal(PullRequestState.Closed, second.State);
        Assert.Equal(first.Number, second.Number);
    }

    [Fact]
    public async Task ClosePullRequest_WhenMerged_ReturnsMergedPullRequest()
    {
        await using var provider = CreateProvider();
        var (repository, pullRequest) = await SeedRepositoryWithPullRequestAsync(
            provider,
            "close-merged");
        await provider.MergePullRequestAsync(repository, pullRequest.Number);

        var closed = await provider.ClosePullRequestAsync(repository, pullRequest.Number);

        Assert.Equal(PullRequestState.Merged, closed.State);
    }

    [Fact]
    public async Task MergePullRequest_UnsupportedMethod_Throws()
    {
        await using var provider = CreateProviderWithUnsupportedMergeMethod();
        var (repository, pullRequest) = await SeedRepositoryWithPullRequestAsync(
            provider,
            "unsupported-merge");

        await Assert.ThrowsAsync<VcsUnsupportedOperationException>(
            () => provider.MergePullRequestAsync(
                repository,
                pullRequest.Number,
                new MergePullRequestRequest { Method = UnsupportedMergeMethod }));
    }

    [Fact]
    public async Task MergePullRequest_Conflict_Throws()
    {
        await using var provider = CreateProviderWithMergeConflict();
        var (repository, pullRequest) = await SeedRepositoryWithPullRequestAsync(
            provider,
            "merge-conflict");

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.MergePullRequestAsync(repository, pullRequest.Number));
    }

    /// <summary>
    /// Creates a repository reference for the conformance scenario.
    /// </summary>
    protected virtual RepositoryRef CreateRepositoryRef(string name)
    {
        return new RepositoryRef { Owner = "conformance", Name = name };
    }

    /// <summary>
    /// Creates a repository request for the conformance scenario.
    /// </summary>
    protected virtual CreateRepositoryRequest CreateRepositoryRequest(string name)
    {
        return new CreateRepositoryRequest
        {
            Owner = "conformance",
            Name = name,
            DefaultBranch = "main",
        };
    }

    /// <summary>
    /// Creates a pull request request for the conformance scenario.
    /// </summary>
    protected virtual CreatePullRequestRequest CreatePullRequestRequest(string sourceBranch)
    {
        return new CreatePullRequestRequest
        {
            Title = "Conformance change",
            SourceBranch = sourceBranch,
            TargetBranch = "main",
        };
    }

    private async Task<(RepositoryRef Repository, PullRequest PullRequest)>
        SeedRepositoryWithPullRequestAsync(IVcsProvider provider, string name)
    {
        var repository = CreateRepositoryRef(name);
        await provider.CreateRepositoryAsync(CreateRepositoryRequest(name));

        var pullRequest = await provider.CreatePullRequestAsync(
            repository,
            CreatePullRequestRequest($"{name}-feature"));

        return (repository, pullRequest);
    }
}
