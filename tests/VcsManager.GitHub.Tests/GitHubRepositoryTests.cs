using System.Net;
using NSubstitute;
using Octokit;
using VcsManager.GitHub.Internal;

namespace VcsManager.GitHub.Tests;

public sealed class GitHubRepositoryTests
{
    [Fact]
    public async Task CreateRepository_WithoutOwner_CreatesForAuthenticatedUser()
    {
        var (provider, client) = CreateProvider();
        var request = new CreateRepositoryRequest
        {
            Name = "test-repo",
            Description = "Test repository",
            Visibility = VcsVisibility.Public,
            InitializeWithReadme = true,
            DefaultBranch = "main",
        };

        client.CreateRepositoryAsync(null, request, Arg.Any<CancellationToken>())
            .Returns(TestData.Repository(visibility: VcsVisibility.Public));

        var repository = await provider.CreateRepositoryAsync(request);

        Assert.Equal("test-owner", repository.Owner);
        Assert.Equal("test-repo", repository.Name);
        Assert.Equal("42", repository.Id);
        Assert.Equal(VcsVisibility.Public, repository.Visibility);
        await client.Received(1).CreateRepositoryAsync(
            null,
            Arg.Is<CreateRepositoryRequest>(value =>
                value.Name == request.Name &&
                value.InitializeWithReadme &&
                value.DefaultBranch == "main"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRepository_WithOwner_CreatesForOrganization()
    {
        var (provider, client) = CreateProvider();
        var request = new CreateRepositoryRequest { Name = "test-repo", Owner = "test-org" };

        client.CreateRepositoryAsync("test-org", request, Arg.Any<CancellationToken>())
            .Returns(TestData.Repository(owner: "test-org"));

        await provider.CreateRepositoryAsync(request);

        await client.Received(1).CreateRepositoryAsync(
            "test-org",
            request,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRepository_WithDefaultOwner_UsesConfiguredOwner()
    {
        var (provider, client) = CreateProvider(new GitHubOptions
        {
            Token = "test-token",
            DefaultOwner = "default-owner",
        });
        var request = new CreateRepositoryRequest { Name = "test-repo" };

        client.CreateRepositoryAsync("default-owner", request, Arg.Any<CancellationToken>())
            .Returns(TestData.Repository(owner: "default-owner"));

        await provider.CreateRepositoryAsync(request);

        await client.Received(1).CreateRepositoryAsync(
            "default-owner",
            request,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRepository_WithProject_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.CreateRepositoryAsync(
            new CreateRepositoryRequest { Name = "test-repo", Project = "group" }));
    }

    [Fact]
    public async Task CreateRepository_WithMissingName_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.CreateRepositoryAsync(
            new CreateRepositoryRequest { Name = " " }));
    }

    [Theory]
    [InlineData("bad/name")]
    [InlineData("bad name")]
    [InlineData("bad?name")]
    [InlineData("bad%2Fname")]
    [InlineData("bad\\name")]
    [InlineData(".")]
    [InlineData("..")]
    public async Task CreateRepository_WithInvalidName_ThrowsArgumentException(string name)
    {
        var (provider, client) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.CreateRepositoryAsync(new CreateRepositoryRequest { Name = name }));

        await client.DidNotReceiveWithAnyArgs().CreateRepositoryAsync(
            default,
            default!,
            default);
    }

    [Theory]
    [InlineData("bad/owner")]
    [InlineData("bad owner")]
    [InlineData("..")]
    public async Task CreateRepository_WithInvalidOwner_ThrowsArgumentException(string owner)
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.CreateRepositoryAsync(
            new CreateRepositoryRequest { Name = "test-repo", Owner = owner }));
    }

    [Theory]
    [InlineData("test-owner", "bad/name")]
    [InlineData("test-owner", "bad name")]
    [InlineData("test-owner", "..")]
    [InlineData("test-owner", ".")]
    [InlineData("bad/owner", "test-repo")]
    [InlineData("bad?owner", "test-repo")]
    [InlineData("..", "test-repo")]
    public async Task GetRepository_WithInvalidPathSegment_ThrowsArgumentException(string owner, string name)
    {
        var (provider, client) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetRepositoryAsync(
            new RepositoryRef { Owner = owner, Name = name }));

        await client.DidNotReceiveWithAnyArgs().GetRepositoryAsync(default!, default!, default);
    }

    [Fact]
    public async Task GetRepository_WithInvalidDefaultOwner_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider(new GitHubOptions
        {
            Token = "test-token",
            DefaultOwner = "bad/owner",
        });

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.GetRepositoryAsync(new RepositoryRef { Name = "test-repo" }));
    }

    [Fact]
    public async Task CreateRepository_WithPaddedName_SendsTrimmedName()
    {
        var (provider, client) = CreateProvider();
        var request = new CreateRepositoryRequest { Name = "  test-repo  ", Owner = "test-org" };

        client.CreateRepositoryAsync("test-org", Arg.Any<CreateRepositoryRequest>(), Arg.Any<CancellationToken>())
            .Returns(TestData.Repository(owner: "test-org"));

        await provider.CreateRepositoryAsync(request);

        await client.Received(1).CreateRepositoryAsync(
            "test-org",
            Arg.Is<CreateRepositoryRequest>(value => value.Name == "test-repo"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRepository_InternalVisibilityUnsupported_ThrowsUnsupportedOperation()
    {
        var (provider, client) = CreateProvider();
        var request = new CreateRepositoryRequest
        {
            Name = "test-repo",
            Visibility = VcsVisibility.Internal,
        };

        client.CreateRepositoryAsync(null, request, Arg.Any<CancellationToken>())
            .Returns<Task<GitHubRepository>>(_ => throw new ApiException(
                "Visibility can't be internal",
                HttpStatusCode.UnprocessableEntity));

        await Assert.ThrowsAsync<VcsUnsupportedOperationException>(
            () => provider.CreateRepositoryAsync(request));
    }

    [Fact]
    public async Task CreateRepository_Conflict_ThrowsVcsConflictException()
    {
        var (provider, client) = CreateProvider();
        var request = new CreateRepositoryRequest { Name = "test-repo" };

        client.CreateRepositoryAsync(null, request, Arg.Any<CancellationToken>())
            .Returns<Task<GitHubRepository>>(_ => throw new ApiException(
                "Repository already exists",
                HttpStatusCode.UnprocessableEntity));

        await Assert.ThrowsAsync<VcsConflictException>(() => provider.CreateRepositoryAsync(request));
    }

    [Fact]
    public async Task GetRepository_WithoutOwner_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.GetRepositoryAsync(new RepositoryRef { Name = "test-repo" }));
    }

    [Fact]
    public async Task GetRepository_WithProject_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetRepositoryAsync(
            new RepositoryRef { Owner = "test-owner", Project = "group", Name = "test-repo" }));
    }

    [Fact]
    public async Task GetRepository_Missing_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();
        var repository = new RepositoryRef { Owner = "test-owner", Name = "missing" };

        client.GetRepositoryAsync("test-owner", "missing", Arg.Any<CancellationToken>())
            .Returns<Task<GitHubRepository>>(_ => throw new ApiException(
                "Not Found",
                HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.GetRepositoryAsync(repository));
    }

    [Fact]
    public async Task GetRepository_MapsValues()
    {
        var (provider, client) = CreateProvider();
        var repository = new RepositoryRef { Owner = "test-owner", Name = "test-repo" };

        client.GetRepositoryAsync("test-owner", "test-repo", Arg.Any<CancellationToken>())
            .Returns(TestData.Repository());

        var result = await provider.GetRepositoryAsync(repository);

        Assert.Equal("test-owner/test-repo", result.FullName);
        Assert.Equal("main", result.DefaultBranch);
        Assert.Equal(VcsVisibility.Private, result.Visibility);
        Assert.Equal(DateTimeOffset.UnixEpoch, result.CreatedAt);
    }

    [Fact]
    public async Task RepositoryExists_WhenMissing_ReturnsFalse()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync("test-owner", "missing", Arg.Any<CancellationToken>())
            .Returns<Task<GitHubRepository>>(_ => throw new ApiException(
                "Not Found",
                HttpStatusCode.NotFound));

        var exists = await provider.RepositoryExistsAsync(
            new RepositoryRef { Owner = "test-owner", Name = "missing" });

        Assert.False(exists);
    }

    [Fact]
    public async Task RepositoryExists_WhenFound_ReturnsTrue()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync("test-owner", "test-repo", Arg.Any<CancellationToken>())
            .Returns(TestData.Repository());

        var exists = await provider.RepositoryExistsAsync(
            new RepositoryRef { Owner = "test-owner", Name = "test-repo" });

        Assert.True(exists);
    }

    [Fact]
    public async Task DeleteRepository_Missing_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.DeleteRepositoryAsync("test-owner", "missing", Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ApiException("Not Found", HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(() => provider.DeleteRepositoryAsync(
            new RepositoryRef { Owner = "test-owner", Name = "missing" }));
    }

    [Fact]
    public async Task ListRepositories_WithoutOwner_ListsAuthenticatedUser()
    {
        var (provider, client) = CreateProvider();
        var query = new RepositoryQuery { NameContains = "test" };

        client.ListRepositoriesAsync(
                null,
                query,
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage([TestData.Repository()]));

        var result = await provider.ListRepositoriesAsync(query);

        Assert.Single(result.Items);
        Assert.Null(result.TotalCount);
        await client.Received(1).ListRepositoriesAsync(
            null,
            query,
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRepositories_WithOwner_ListsOrganization()
    {
        var (provider, client) = CreateProvider();
        var query = new RepositoryQuery { Owner = "test-org" };

        client.ListRepositoriesAsync(
                "test-org",
                query,
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage([]));

        await provider.ListRepositoriesAsync(query);

        await client.Received(1).ListRepositoriesAsync(
            "test-org",
            query,
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRepositories_WithDefaultOwner_UsesConfiguredOwner()
    {
        var (provider, client) = CreateProvider(new GitHubOptions
        {
            Token = "test-token",
            DefaultOwner = "default-owner",
        });

        client.ListRepositoriesAsync(
                "default-owner",
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage([]));

        await provider.ListRepositoriesAsync();

        await client.Received(1).ListRepositoriesAsync(
            "default-owner",
            Arg.Any<RepositoryQuery>(),
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRepositories_AuthorizationFailure_ThrowsVcsAuthorizationException()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                null,
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GitHubPage<GitHubRepository>>>(_ => throw new ApiException(
                "Forbidden",
                HttpStatusCode.Forbidden));

        await Assert.ThrowsAsync<VcsAuthorizationException>(
            () => provider.ListRepositoriesAsync());
    }

    [Fact]
    public async Task ListRepositories_WithProject_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.ListRepositoriesAsync(new RepositoryQuery { Project = "group" }));
    }

    [Fact]
    public async Task ListRepositories_MapsPaginationMetadata()
    {
        var (provider, client) = CreateProvider();
        var pagination = new PaginationOptions { Page = 2, PageSize = 5 };

        client.ListRepositoriesAsync(
                null,
                Arg.Any<RepositoryQuery>(),
                pagination,
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage([TestData.Repository()], hasNextPage: true));

        var result = await provider.ListRepositoriesAsync(pagination: pagination);

        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.True(result.HasNextPage);
        Assert.Null(result.TotalCount);
    }

    [Fact]
    public async Task ListRepositories_AppliesVisibilityNameAndArchivedFilters()
    {
        var (provider, client) = CreateProvider();
        var query = new RepositoryQuery
        {
            NameContains = "alpha",
            Visibility = VcsVisibility.Private,
        };

        client.ListRepositoriesAsync(
                null,
                query,
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage(
            [
                TestData.Repository(name: "alpha"),
                TestData.Repository(name: "beta"),
                TestData.Repository(name: "alpha-archived", archived: true),
                TestData.Repository(name: "alpha-public", visibility: VcsVisibility.Public),
            ]));

        var result = await provider.ListRepositoriesAsync(query);

        var repository = Assert.Single(result.Items);
        Assert.Equal("alpha", repository.Name);
    }

    [Fact]
    public async Task ListRepositories_IncludeArchived_ReturnsArchivedRepositories()
    {
        var (provider, client) = CreateProvider();
        var query = new RepositoryQuery { IncludeArchived = true };

        client.ListRepositoriesAsync(
                null,
                query,
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage(
            [
                TestData.Repository(name: "active"),
                TestData.Repository(name: "archived", archived: true),
            ]));

        var result = await provider.ListRepositoriesAsync(query);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListRepositories_PaddedNameContains_IsNormalized()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                null,
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage(
            [
                TestData.Repository(name: "alpha"),
                TestData.Repository(name: "beta"),
            ]));

        var result = await provider.ListRepositoriesAsync(
            new RepositoryQuery { NameContains = " alpha " });

        var repository = Assert.Single(result.Items);
        Assert.Equal("alpha", repository.Name);
        await client.Received(1).ListRepositoriesAsync(
            null,
            Arg.Is<RepositoryQuery>(query => query.NameContains == "alpha"),
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    private static (GitHubProvider Provider, IGitHubApiClient Client) CreateProvider(
        GitHubOptions? options = null)
    {
        var client = Substitute.For<IGitHubApiClient>();
        return (
            new GitHubProvider(client, options ?? new GitHubOptions { Token = "test-token" }),
            client);
    }
}
