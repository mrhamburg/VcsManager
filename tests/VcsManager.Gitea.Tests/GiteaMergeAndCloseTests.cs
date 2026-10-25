using System.Net;
using NSubstitute;
using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaMergeAndCloseTests
{
    [Theory]
    [InlineData(MergeMethod.Merge)]
    [InlineData(MergeMethod.Squash)]
    [InlineData(MergeMethod.Rebase)]
    public async Task MergePullRequest_SendsSelectedMethod(MergeMethod method)
    {
        var (provider, client) = CreateProvider();
        var request = new MergePullRequestRequest
        {
            Method = method,
            DeleteSourceBranch = true,
            CommitTitle = "Complete change",
            CommitMessage = "Completed by VcsManager",
        };

        client.GetPullRequestAsync(
                TestData.Owner,
                TestData.RepositoryName,
                7,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest());

        client.MergePullRequestAsync(
                TestData.Owner,
                TestData.RepositoryName,
                7,
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Merged));

        var merged = await provider.MergePullRequestAsync(
            GiteaPullRequestTests.CreateRepositoryRef(),
            7,
            request);

        Assert.Equal(PullRequestState.Merged, merged.State);
        await client.Received(1).MergePullRequestAsync(
            TestData.Owner,
            TestData.RepositoryName,
            7,
            Arg.Is<MergePullRequestRequest>(value =>
                value.Method == method &&
                value.DeleteSourceBranch &&
                value.CommitTitle == request.CommitTitle &&
                value.CommitMessage == request.CommitMessage),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.Locked)]
    public async Task MergePullRequest_RejectedByRepository_ThrowsConflict(
        HttpStatusCode statusCode)
    {
        var (provider, client) = CreateProvider();
        SetupActivePullRequest(client);

        client.MergePullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<MergePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GiteaPullRequest>>(_ => throw TestData.ApiException(
                statusCode,
                "{\"message\":\"merge style disabled\"}"));

        var exception = await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.MergePullRequestAsync(
                GiteaPullRequestTests.CreateRepositoryRef(),
                7));

        Assert.Contains("MergePullRequest", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MergePullRequest_Missing_ThrowsPullRequestNotFoundException()
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
            () => provider.MergePullRequestAsync(
                GiteaPullRequestTests.CreateRepositoryRef(),
                7));
    }

    [Fact]
    public async Task MergePullRequest_WhenAlreadyMerged_ReturnsUnchanged()
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Merged));

        var merged = await provider.MergePullRequestAsync(
            GiteaPullRequestTests.CreateRepositoryRef(),
            7);

        Assert.Equal(PullRequestState.Merged, merged.State);
        await client.DidNotReceive().MergePullRequestAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<MergePullRequestRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergePullRequest_WhenClosed_ThrowsConflict()
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Closed));

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.MergePullRequestAsync(
                GiteaPullRequestTests.CreateRepositoryRef(),
                7));
    }

    [Fact]
    public async Task ClosePullRequest_ClosesAndReturnsClosed()
    {
        var (provider, client) = CreateProvider();
        SetupActivePullRequest(client);

        client.ClosePullRequestAsync(
                TestData.Owner,
                TestData.RepositoryName,
                7,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Closed));

        var closed = await provider.ClosePullRequestAsync(
            GiteaPullRequestTests.CreateRepositoryRef(),
            7);

        Assert.Equal(PullRequestState.Closed, closed.State);
        Assert.Equal(7, closed.Number);
    }

    [Theory]
    [InlineData(PullRequestState.Closed)]
    [InlineData(PullRequestState.Merged)]
    public async Task ClosePullRequest_WhenAlreadyClosedOrMerged_DoesNotWrite(
        PullRequestState state)
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: state));

        var closed = await provider.ClosePullRequestAsync(
            GiteaPullRequestTests.CreateRepositoryRef(),
            7);

        Assert.Equal(state, closed.State);
        await client.DidNotReceive().ClosePullRequestAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClosePullRequest_IsIdempotent()
    {
        var (provider, client) = CreateProvider();
        SetupActivePullRequest(client);

        client.ClosePullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Closed));

        var first = await provider.ClosePullRequestAsync(
            GiteaPullRequestTests.CreateRepositoryRef(),
            7);

        client.GetPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Closed));

        var second = await provider.ClosePullRequestAsync(
            GiteaPullRequestTests.CreateRepositoryRef(),
            7);

        Assert.Equal(PullRequestState.Closed, first.State);
        Assert.Equal(PullRequestState.Closed, second.State);
        await client.Received(1).ClosePullRequestAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClosePullRequest_Missing_ThrowsPullRequestNotFoundException()
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
            () => provider.ClosePullRequestAsync(
                GiteaPullRequestTests.CreateRepositoryRef(),
                7));
    }

    private static void SetupActivePullRequest(IGiteaApiClient client)
    {
        client.GetPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest());
    }

    private static (GiteaProvider Provider, IGiteaApiClient Client) CreateProvider()
    {
        return GiteaPullRequestTests.CreateProvider();
    }
}
