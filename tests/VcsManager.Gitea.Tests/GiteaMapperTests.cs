using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaMapperTests
{
    [Theory]
    [InlineData("open", false, false, PullRequestState.Open)]
    [InlineData("open", true, false, PullRequestState.Draft)]
    [InlineData("closed", false, false, PullRequestState.Closed)]
    [InlineData("closed", false, true, PullRequestState.Merged)]
    [InlineData("closed", true, true, PullRequestState.Merged)]
    public void FromPullRequestResponse_MapsStateAndMergedPrecedence(
        string state,
        bool draft,
        bool merged,
        PullRequestState expected)
    {
        var response = CreateResponse(state, draft, merged, "Test pull request");

        var mapped = GiteaMapper.FromPullRequestResponse(response);

        Assert.Equal(expected, mapped.State);
        Assert.Equal(draft, mapped.IsDraft);
    }

    [Fact]
    public void FromPullRequestResponse_WithWipTitle_IsDraft()
    {
        var response = CreateResponse("open", draft: false, merged: false, "WIP: Draft change");

        var mapped = GiteaMapper.FromPullRequestResponse(response);

        Assert.True(mapped.IsDraft);
        Assert.Equal(PullRequestState.Draft, mapped.State);
    }

    [Fact]
    public void FromPullRequestResponse_MapsBranchesAuthorAndWebUrl()
    {
        var response = CreateResponse("open", draft: false, merged: false, "Test pull request");

        var mapped = GiteaMapper.FromPullRequestResponse(response);

        Assert.Equal(7, mapped.Number);
        Assert.Equal("feature", mapped.SourceBranch);
        Assert.Equal("main", mapped.TargetBranch);
        Assert.Equal("test-user", mapped.Author);
        Assert.Equal("A test pull request", mapped.Description);
    }

    [Fact]
    public void FromRepositoryResponse_MapsVisibilityOwnerAndArchive()
    {
        var response = new GiteaRepositoryResponse
        {
            Id = 42,
            Name = "test-repository",
            FullName = "test-owner/test-repository",
            HtmlUrl = "https://gitea.example.com/test-owner/test-repository",
            DefaultBranch = "main",
            Private = false,
            Archived = true,
            Owner = new GiteaUserResponse { Id = 1, Login = "test-owner" },
        };

        var mapped = GiteaMapper.FromRepositoryResponse(response);

        Assert.Equal("test-owner", mapped.Owner);
        Assert.Equal(VcsVisibility.Public, mapped.Visibility);
        Assert.True(mapped.IsArchived);
    }

    [Fact]
    public void FromRepositoryResponse_WithoutOwner_UsesFullNamePrefix()
    {
        var response = new GiteaRepositoryResponse
        {
            Id = 42,
            Name = "test-repository",
            FullName = "test-owner/test-repository",
            Private = true,
        };

        var mapped = GiteaMapper.FromRepositoryResponse(response);

        Assert.Equal("test-owner", mapped.Owner);
    }

    private static GiteaPullRequestResponse CreateResponse(
        string state,
        bool draft,
        bool merged,
        string title)
    {
        return new GiteaPullRequestResponse
        {
            Id = 107,
            Number = 7,
            Title = title,
            Body = "A test pull request",
            State = state,
            Draft = draft,
            Merged = merged,
            HtmlUrl = "https://gitea.example.com/test-owner/test-repository/pulls/7",
            User = new GiteaUserResponse { Id = 2, Login = "test-user" },
            Head = new GiteaPullRequestBranchResponse { Ref = "feature" },
            Base = new GiteaPullRequestBranchResponse { Ref = "main" },
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            MergedAt = merged ? DateTimeOffset.UnixEpoch : null,
            ClosedAt = state == "closed" ? DateTimeOffset.UnixEpoch : null,
        };
    }
}
