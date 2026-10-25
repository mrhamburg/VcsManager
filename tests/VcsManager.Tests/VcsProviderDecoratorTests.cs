using NSubstitute;

namespace VcsManager.Tests;

public class VcsProviderDecoratorTests
{
    [Fact]
    public async Task ForwardsEveryMethodToInnerProvider()
    {
        var inner = Substitute.For<IVcsProvider>();
        var repositoryRef = new RepositoryRef { Name = "core" };
        var repository = new VcsRepository
        {
            Id = "1",
            Name = "core",
            FullName = "core",
            Url = "https://example.test/core",
        };
        var createRepositoryRequest = new CreateRepositoryRequest { Name = "core" };
        var seedRepositoryRequest = new SeedRepositoryRequest
        {
            Branch = "main",
            Files =
            [
                new RepositorySeedFile
                {
                    Path = "README.md",
                    Content = "seed"u8.ToArray(),
                },
            ],
        };
        var createPullRequestRequest = new CreatePullRequestRequest
        {
            Title = "Add feature",
            SourceBranch = "feature",
            TargetBranch = "main",
        };
        var pullRequest = new PullRequest
        {
            Id = "1",
            Number = 1,
            Title = "Add feature",
            SourceBranch = "feature",
            TargetBranch = "main",
            Author = "author",
            Url = "https://example.test/core/pull/1",
        };
        var repositoryPage = new PagedResult<VcsRepository> { Items = [repository] };
        var pullRequestPage = new PagedResult<PullRequest> { Items = [pullRequest] };
        var repositoryQuery = new RepositoryQuery();
        var pullRequestQuery = new PullRequestQuery();
        var pagination = new PaginationOptions();
        var mergeRequest = new MergePullRequestRequest();

        inner.CreateRepositoryAsync(createRepositoryRequest, Arg.Any<CancellationToken>())
            .Returns(repository);
        inner.SeedRepositoryAsync(repositoryRef, seedRepositoryRequest, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        inner.GetRepositoryAsync(repositoryRef, Arg.Any<CancellationToken>())
            .Returns(repository);
        inner.ListRepositoriesAsync(
                repositoryQuery,
                pagination,
                Arg.Any<CancellationToken>())
            .Returns(repositoryPage);
        inner.RepositoryExistsAsync(repositoryRef, Arg.Any<CancellationToken>())
            .Returns(true);
        inner.CreatePullRequestAsync(repositoryRef, createPullRequestRequest, Arg.Any<CancellationToken>())
            .Returns(pullRequest);
        inner.GetPullRequestAsync(repositoryRef, 1, Arg.Any<CancellationToken>())
            .Returns(pullRequest);
        inner.ListPullRequestsAsync(
                repositoryRef,
                pullRequestQuery,
                pagination,
                Arg.Any<CancellationToken>())
            .Returns(pullRequestPage);
        inner.MergePullRequestAsync(repositoryRef, 1, mergeRequest, Arg.Any<CancellationToken>())
            .Returns(pullRequest);
        inner.ClosePullRequestAsync(repositoryRef, 1, Arg.Any<CancellationToken>())
            .Returns(pullRequest);

        var decorator = new PassThroughDecorator(inner);

        Assert.Same(repository, await decorator.CreateRepositoryAsync(createRepositoryRequest));
        await decorator.SeedRepositoryAsync(repositoryRef, seedRepositoryRequest);
        Assert.Same(repository, await decorator.GetRepositoryAsync(repositoryRef));
        Assert.Same(repositoryPage, await decorator.ListRepositoriesAsync(repositoryQuery, pagination));
        await decorator.DeleteRepositoryAsync(repositoryRef);
        Assert.True(await decorator.RepositoryExistsAsync(repositoryRef));
        Assert.Same(pullRequest, await decorator.CreatePullRequestAsync(repositoryRef, createPullRequestRequest));
        Assert.Same(pullRequest, await decorator.GetPullRequestAsync(repositoryRef, 1));
        Assert.Same(
            pullRequestPage,
            await decorator.ListPullRequestsAsync(repositoryRef, pullRequestQuery, pagination));
        Assert.Same(pullRequest, await decorator.MergePullRequestAsync(repositoryRef, 1, mergeRequest));
        Assert.Same(pullRequest, await decorator.ClosePullRequestAsync(repositoryRef, 1));

        await inner.Received(1).CreateRepositoryAsync(createRepositoryRequest, Arg.Any<CancellationToken>());
        await inner.Received(1).SeedRepositoryAsync(
            repositoryRef,
            seedRepositoryRequest,
            Arg.Any<CancellationToken>());
        await inner.Received(1).GetRepositoryAsync(repositoryRef, Arg.Any<CancellationToken>());
        await inner.Received(1).ListRepositoriesAsync(
            repositoryQuery,
            pagination,
            Arg.Any<CancellationToken>());
        await inner.Received(1).DeleteRepositoryAsync(repositoryRef, Arg.Any<CancellationToken>());
        await inner.Received(1).RepositoryExistsAsync(repositoryRef, Arg.Any<CancellationToken>());
        await inner.Received(1).CreatePullRequestAsync(
            repositoryRef,
            createPullRequestRequest,
            Arg.Any<CancellationToken>());
        await inner.Received(1).GetPullRequestAsync(repositoryRef, 1, Arg.Any<CancellationToken>());
        await inner.Received(1).ListPullRequestsAsync(
            repositoryRef,
            pullRequestQuery,
            pagination,
            Arg.Any<CancellationToken>());
        await inner.Received(1).MergePullRequestAsync(
            repositoryRef,
            1,
            mergeRequest,
            Arg.Any<CancellationToken>());
        await inner.Received(1).ClosePullRequestAsync(repositoryRef, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisposeAsync_ForwardsExactlyOnce()
    {
        var inner = Substitute.For<IVcsProvider>();
        var decorator = new PassThroughDecorator(inner);

        await decorator.DisposeAsync();

        await inner.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task MultipleLayers_RecordCallOrder()
    {
        var calls = new List<string>();
        await using var provider = new TrackingDecorator(
            new TrackingDecorator(new FakeVcsProvider(), "second", calls),
            "first",
            calls);

        await provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" });

        Assert.Equal(
            new[] { "first-enter", "second-enter", "second-exit", "first-exit" },
            calls);
    }

    private sealed class PassThroughDecorator(IVcsProvider inner) : VcsProviderDecorator(inner)
    {
    }

    private sealed class TrackingDecorator(IVcsProvider inner, string name, List<string> calls)
        : VcsProviderDecorator(inner)
    {
        public override async Task<bool> RepositoryExistsAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            calls.Add($"{name}-enter");
            var result = await base.RepositoryExistsAsync(repository, cancellationToken);
            calls.Add($"{name}-exit");
            return result;
        }
    }
}
