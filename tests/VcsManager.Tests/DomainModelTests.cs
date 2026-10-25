namespace VcsManager.Tests;

public class DomainModelTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAt =
        new(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    public void RepositoryRef_OptionalFieldsDefaultToNull()
    {
        var repository = new RepositoryRef { Name = "core" };

        Assert.Equal("core", repository.Name);
        Assert.Null(repository.Owner);
        Assert.Null(repository.Project);
    }

    [Fact]
    public void VcsRepository_PreservesValues()
    {
        var repository = new VcsRepository
        {
            Id = "1",
            Name = "core",
            Owner = "acme",
            Project = "platform",
            FullName = "acme/platform/core",
            Url = "https://example.test/acme/platform/core",
            DefaultBranch = "main",
            Visibility = VcsVisibility.Public,
            Description = "Core repository",
            IsArchived = true,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };

        Assert.Equal("1", repository.Id);
        Assert.Equal("core", repository.Name);
        Assert.Equal("acme", repository.Owner);
        Assert.Equal("platform", repository.Project);
        Assert.Equal("acme/platform/core", repository.FullName);
        Assert.Equal("https://example.test/acme/platform/core", repository.Url);
        Assert.Equal("main", repository.DefaultBranch);
        Assert.Equal(VcsVisibility.Public, repository.Visibility);
        Assert.Equal("Core repository", repository.Description);
        Assert.True(repository.IsArchived);
        Assert.Equal(CreatedAt, repository.CreatedAt);
        Assert.Equal(UpdatedAt, repository.UpdatedAt);
    }

    [Fact]
    public void VcsRepository_Defaults()
    {
        var repository = new VcsRepository
        {
            Id = "1",
            Name = "core",
            FullName = "core",
            Url = "https://example.test/core",
        };

        Assert.Null(repository.Owner);
        Assert.Null(repository.Project);
        Assert.Null(repository.DefaultBranch);
        Assert.Null(repository.Description);
        Assert.Null(repository.CreatedAt);
        Assert.Null(repository.UpdatedAt);
        Assert.Equal(VcsVisibility.Private, repository.Visibility);
        Assert.False(repository.IsArchived);
    }

    [Fact]
    public void PullRequest_PreservesValues()
    {
        var closedAt = CreatedAt.AddHours(1);
        var pullRequest = new PullRequest
        {
            Id = "7",
            Number = 7,
            Title = "Add feature",
            Description = "Description",
            SourceBranch = "feature",
            TargetBranch = "main",
            State = PullRequestState.Merged,
            IsDraft = true,
            Author = "author",
            Url = "https://example.test/core/pull/7",
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            MergedAt = UpdatedAt,
            ClosedAt = closedAt,
        };

        Assert.Equal("7", pullRequest.Id);
        Assert.Equal(7, pullRequest.Number);
        Assert.Equal("Add feature", pullRequest.Title);
        Assert.Equal("Description", pullRequest.Description);
        Assert.Equal("feature", pullRequest.SourceBranch);
        Assert.Equal("main", pullRequest.TargetBranch);
        Assert.Equal(PullRequestState.Merged, pullRequest.State);
        Assert.True(pullRequest.IsDraft);
        Assert.Equal("author", pullRequest.Author);
        Assert.Equal("https://example.test/core/pull/7", pullRequest.Url);
        Assert.Equal(CreatedAt, pullRequest.CreatedAt);
        Assert.Equal(UpdatedAt, pullRequest.UpdatedAt);
        Assert.Equal(UpdatedAt, pullRequest.MergedAt);
        Assert.Equal(closedAt, pullRequest.ClosedAt);
    }

    [Fact]
    public void PullRequest_Defaults()
    {
        var pullRequest = new PullRequest
        {
            Id = "7",
            Number = 7,
            Title = "Add feature",
            SourceBranch = "feature",
            TargetBranch = "main",
            Author = "author",
            Url = "https://example.test/core/pull/7",
        };

        Assert.Null(pullRequest.Description);
        Assert.Null(pullRequest.CreatedAt);
        Assert.Null(pullRequest.UpdatedAt);
        Assert.Null(pullRequest.MergedAt);
        Assert.Null(pullRequest.ClosedAt);
        Assert.Equal(PullRequestState.Open, pullRequest.State);
        Assert.False(pullRequest.IsDraft);
    }

    [Fact]
    public void CreateRepositoryRequest_Defaults()
    {
        var request = new CreateRepositoryRequest { Name = "core" };

        Assert.Null(request.Owner);
        Assert.Null(request.Project);
        Assert.Null(request.Description);
        Assert.Null(request.DefaultBranch);
        Assert.Equal(VcsVisibility.Private, request.Visibility);
        Assert.False(request.InitializeWithReadme);
    }

    [Fact]
    public void RepositoryQuery_Defaults()
    {
        var query = new RepositoryQuery();

        Assert.Null(query.Owner);
        Assert.Null(query.Project);
        Assert.Null(query.Visibility);
        Assert.Null(query.NameContains);
        Assert.False(query.IncludeArchived);
    }

    [Fact]
    public void CreatePullRequestRequest_Defaults()
    {
        var request = new CreatePullRequestRequest
        {
            Title = "Add feature",
            SourceBranch = "feature",
            TargetBranch = "main",
        };

        Assert.Null(request.Description);
        Assert.False(request.Draft);
    }

    [Fact]
    public void PullRequestQuery_Defaults()
    {
        var query = new PullRequestQuery();

        Assert.Null(query.State);
        Assert.Null(query.Author);
        Assert.Null(query.SourceBranch);
        Assert.Null(query.TargetBranch);
    }

    [Fact]
    public void MergePullRequestRequest_Defaults()
    {
        var request = new MergePullRequestRequest();

        Assert.Equal(MergeMethod.Merge, request.Method);
        Assert.False(request.DeleteSourceBranch);
        Assert.Null(request.CommitTitle);
        Assert.Null(request.CommitMessage);
    }
}
