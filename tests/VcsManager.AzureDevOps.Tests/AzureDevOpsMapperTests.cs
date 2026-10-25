using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps.Tests;

public sealed class AzureDevOpsMapperTests
{
    [Theory]
    [InlineData(PullRequestStatus.Active, false, PullRequestState.Open)]
    [InlineData(PullRequestStatus.Active, true, PullRequestState.Draft)]
    [InlineData(PullRequestStatus.Abandoned, false, PullRequestState.Closed)]
    [InlineData(PullRequestStatus.Completed, false, PullRequestState.Merged)]
    public void FromSdkPullRequest_MapsState(
        PullRequestStatus status,
        bool isDraft,
        PullRequestState expected)
    {
        var pullRequest = CreateSdkPullRequest(status, isDraft);

        var mapped = AzureDevOpsMapper.FromSdkPullRequest(
            pullRequest,
            "https://dev.azure.com/test-organization",
            TestData.Project,
            "test-repository");

        Assert.Equal(expected, mapped.State);
        Assert.Equal(isDraft, mapped.IsDraft);
        Assert.Equal("feature", mapped.SourceBranch);
        Assert.Equal("main", mapped.TargetBranch);
        Assert.Equal("Test User", mapped.Author);
        Assert.Equal("2222222222222222222222222222222222222222", mapped.SourceCommitId);
    }

    [Fact]
    public void FromSdkPullRequest_WhenClosed_SetsClosedAt()
    {
        var pullRequest = CreateSdkPullRequest(PullRequestStatus.Abandoned, isDraft: false);

        var mapped = AzureDevOpsMapper.FromSdkPullRequest(
            pullRequest,
            "https://dev.azure.com/test-organization",
            TestData.Project,
            "test-repository");

        Assert.Equal(DateTimeOffset.UnixEpoch, mapped.ClosedAt);
        Assert.Null(mapped.MergedAt);
    }

    [Fact]
    public void FromSdkPullRequest_WhenCompleted_SetsMergedAt()
    {
        var pullRequest = CreateSdkPullRequest(PullRequestStatus.Completed, isDraft: false);

        var mapped = AzureDevOpsMapper.FromSdkPullRequest(
            pullRequest,
            "https://dev.azure.com/test-organization",
            TestData.Project,
            "test-repository");

        Assert.Equal(DateTimeOffset.UnixEpoch, mapped.MergedAt);
        Assert.Null(mapped.ClosedAt);
    }

    [Fact]
    public void FromSdkPullRequest_EscapesProjectAndRepositoryInWebUrl()
    {
        var pullRequest = CreateSdkPullRequest(PullRequestStatus.Active, isDraft: false);

        var mapped = AzureDevOpsMapper.FromSdkPullRequest(
            pullRequest,
            "https://dev.azure.com/test-organization",
            "My Project",
            "my repo");

        Assert.Equal(
            "https://dev.azure.com/test-organization/My%20Project/_git/my%20repo/pullrequest/7",
            mapped.WebUrl);
    }

    [Fact]
    public void FromSdkPullRequest_WithLocalDateTime_TreatsValueAsUtc()
    {
        var pullRequest = CreateSdkPullRequest(PullRequestStatus.Active, isDraft: false);
        pullRequest.CreationDate = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Local);

        var mapped = AzureDevOpsMapper.FromSdkPullRequest(
            pullRequest,
            "https://dev.azure.com/test-organization",
            TestData.Project,
            "test-repository");

        Assert.Equal(
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            mapped.CreatedAt);
    }

    [Theory]
    [InlineData(MergeMethod.Merge, GitPullRequestMergeStrategy.NoFastForward)]
    [InlineData(MergeMethod.Squash, GitPullRequestMergeStrategy.Squash)]
    [InlineData(MergeMethod.Rebase, GitPullRequestMergeStrategy.Rebase)]
    public void ToMergeStrategy_MapsCoreMethod(
        MergeMethod method,
        GitPullRequestMergeStrategy expected)
    {
        Assert.Equal(expected, AzureDevOpsMapper.ToMergeStrategy(method));
    }

    [Fact]
    public void ToRepository_MapsOrganizationProjectAndDefaultBranch()
    {
        var repository = AzureDevOpsMapper.ToRepository(TestData.Repository());

        Assert.Equal("test-organization", repository.Owner);
        Assert.Equal("test-project", repository.Project);
        Assert.Equal("test-organization/test-project/test-repository", repository.FullName);
        Assert.Equal("main", repository.DefaultBranch);
        Assert.Equal(VcsVisibility.Private, repository.Visibility);
    }

    private static GitPullRequest CreateSdkPullRequest(PullRequestStatus status, bool isDraft)
    {
        return new GitPullRequest
        {
            PullRequestId = 7,
            Title = "Test pull request",
            Description = "A test pull request",
            Status = status,
            IsDraft = isDraft,
            SourceRefName = "refs/heads/feature",
            TargetRefName = "refs/heads/main",
            CreatedBy = new IdentityRef { DisplayName = "Test User" },
            LastMergeSourceCommit = new GitCommitRef
            {
                CommitId = "2222222222222222222222222222222222222222",
            },
            CreationDate = DateTime.UnixEpoch,
            ClosedDate = status == PullRequestStatus.Active
                ? default
                : DateTime.UnixEpoch,
        };
    }
}
