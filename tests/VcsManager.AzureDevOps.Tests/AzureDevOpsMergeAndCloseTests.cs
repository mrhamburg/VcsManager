using System.Net;
using NSubstitute;
using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps.Tests;

public sealed class AzureDevOpsMergeAndCloseTests
{
    private const string SourceCommitId = "2222222222222222222222222222222222222222";

    [Theory]
    [InlineData(MergeMethod.Merge)]
    [InlineData(MergeMethod.Squash)]
    [InlineData(MergeMethod.Rebase)]
    public async Task MergePullRequest_SendsSelectedMethodAndSourceCommit(MergeMethod method)
    {
        var (provider, client) = CreateProvider();
        var repository = AzureDevOpsPullRequestTests.CreateRepositoryRef();
        var request = new MergePullRequestRequest
        {
            Method = method,
            DeleteSourceBranch = true,
            CommitTitle = "Complete change",
            CommitMessage = "Completed by VcsManager",
        };

        client.GetPullRequestAsync(
                TestData.Project,
                "test-repository",
                7,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest());

        client.CompletePullRequestAsync(
                TestData.Project,
                "test-repository",
                7,
                SourceCommitId,
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Merged));

        var merged = await provider.MergePullRequestAsync(repository, 7, request);

        Assert.Equal(PullRequestState.Merged, merged.State);
        await client.Received(1).CompletePullRequestAsync(
            TestData.Project,
            "test-repository",
            7,
            SourceCommitId,
            Arg.Is<MergePullRequestRequest>(value =>
                value.Method == method &&
                value.DeleteSourceBranch &&
                value.CommitTitle == request.CommitTitle &&
                value.CommitMessage == request.CommitMessage),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergePullRequest_Conflict_ThrowsVcsConflictException()
    {
        var (provider, client) = CreateProvider();
        SetupActivePullRequest(client);

        client.CompletePullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<MergePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<AzureDevOpsPullRequest>>(_ =>
                throw AzureDevOpsRepositoryTests.CreateResponseException(
                    HttpStatusCode.Conflict,
                    "The pull request conflicts with the target branch."));

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.MergePullRequestAsync(
                AzureDevOpsPullRequestTests.CreateRepositoryRef(),
                7));
    }

    [Fact]
    public async Task MergePullRequest_InvalidTransition_ThrowsVcsConflictException()
    {
        var (provider, client) = CreateProvider();
        SetupActivePullRequest(client);

        client.CompletePullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<MergePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<AzureDevOpsPullRequest>>(_ =>
                throw AzureDevOpsRepositoryTests.CreateResponseException(
                    HttpStatusCode.BadRequest,
                    "The pull request cannot be completed."));

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.MergePullRequestAsync(
                AzureDevOpsPullRequestTests.CreateRepositoryRef(),
                7));
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
            .Returns<Task<AzureDevOpsPullRequest>>(_ =>
                throw AzureDevOpsRepositoryTests.CreateResponseException(
                    HttpStatusCode.NotFound,
                    "Not Found"));

        await Assert.ThrowsAsync<PullRequestNotFoundException>(
            () => provider.MergePullRequestAsync(
                AzureDevOpsPullRequestTests.CreateRepositoryRef(),
                7));
    }

    [Theory]
    [InlineData(PullRequestState.Closed)]
    [InlineData(PullRequestState.Merged)]
    public async Task MergePullRequest_WhenNotActive_ThrowsVcsConflictException(
        PullRequestState state)
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: state));

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.MergePullRequestAsync(
                AzureDevOpsPullRequestTests.CreateRepositoryRef(),
                7));

        await client.DidNotReceive().CompletePullRequestAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<MergePullRequestRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergePullRequest_WithoutSourceCommit_ThrowsVcsConflictException()
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest() with { SourceCommitId = null });

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.MergePullRequestAsync(
                AzureDevOpsPullRequestTests.CreateRepositoryRef(),
                7));
    }

    [Fact]
    public async Task MergePullRequest_WithInvalidMethod_ThrowsArgumentOutOfRangeException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => provider.MergePullRequestAsync(
                AzureDevOpsPullRequestTests.CreateRepositoryRef(),
                7,
                new MergePullRequestRequest { Method = (MergeMethod)99 }));
    }

    [Fact]
    public async Task ClosePullRequest_AbandonsAndReturnsClosed()
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync(
                TestData.Project,
                "test-repository",
                7,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest());

        client.AbandonPullRequestAsync(
                TestData.Project,
                "test-repository",
                7,
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Closed));

        var closed = await provider.ClosePullRequestAsync(
            AzureDevOpsPullRequestTests.CreateRepositoryRef(),
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
            AzureDevOpsPullRequestTests.CreateRepositoryRef(),
            7);

        Assert.Equal(state, closed.State);
        await client.DidNotReceive().AbandonPullRequestAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClosePullRequest_IsIdempotent()
    {
        var (provider, client) = CreateProvider();

        client.GetPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest());

        client.AbandonPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Closed));

        var first = await provider.ClosePullRequestAsync(
            AzureDevOpsPullRequestTests.CreateRepositoryRef(),
            7);

        client.GetPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest(state: PullRequestState.Closed));

        var second = await provider.ClosePullRequestAsync(
            AzureDevOpsPullRequestTests.CreateRepositoryRef(),
            7);

        Assert.Equal(PullRequestState.Closed, first.State);
        Assert.Equal(PullRequestState.Closed, second.State);
        await client.Received(1).AbandonPullRequestAsync(
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
            .Returns<Task<AzureDevOpsPullRequest>>(_ =>
                throw AzureDevOpsRepositoryTests.CreateResponseException(
                    HttpStatusCode.NotFound,
                    "Not Found"));

        await Assert.ThrowsAsync<PullRequestNotFoundException>(
            () => provider.ClosePullRequestAsync(
                AzureDevOpsPullRequestTests.CreateRepositoryRef(),
                7));
    }

    private static void SetupActivePullRequest(IAzureDevOpsApiClient client)
    {
        client.GetPullRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.PullRequest());
    }

    private static (AzureDevOpsProvider Provider, IAzureDevOpsApiClient Client) CreateProvider()
    {
        return AzureDevOpsPullRequestTests.CreateProvider();
    }
}
