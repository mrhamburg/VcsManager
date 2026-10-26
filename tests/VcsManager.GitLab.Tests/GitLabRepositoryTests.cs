using System.Net;
using NSubstitute;
using VcsManager.GitLab.Internal;

namespace VcsManager.GitLab.Tests;

public sealed class GitLabRepositoryTests
{
    [Fact]
    public async Task CreateRepository_WithDefaultNamespace_UsesConfiguredNamespace()
    {
        var (provider, client) = CreateProvider(new GitLabOptions
        {
            Token = "test-token",
            DefaultNamespace = "default-group",
        });
        var request = new CreateRepositoryRequest { Name = "test-project" };

        client.CreateProjectAsync(
                "default-group",
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.Project(owner: "default-group"));

        var repository = await provider.CreateRepositoryAsync(request);

        Assert.Equal("default-group/test-project", repository.FullName);
        await client.Received(1).CreateProjectAsync(
            "default-group",
            request,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRepository_TrimsNameBeforeCallingClient()
    {
        var (provider, client) = CreateProvider();

        client.CreateProjectAsync(
                Arg.Any<string?>(),
                Arg.Any<CreateRepositoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.Project());

        await provider.CreateRepositoryAsync(
            new CreateRepositoryRequest { Owner = "test-group", Name = "  test-project  " });

        await client.Received(1).CreateProjectAsync(
            "test-group",
            Arg.Is<CreateRepositoryRequest>(value => value.Name == "test-project"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRepository_WithSubgroup_PassesNestedNamespace()
    {
        var (provider, client) = CreateProvider();
        var request = new CreateRepositoryRequest
        {
            Owner = "test-group",
            Project = "subgroup",
            Name = "test-project",
        };

        client.CreateProjectAsync(
                "test-group/subgroup",
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.Project(project: "subgroup"));

        var repository = await provider.CreateRepositoryAsync(request);

        Assert.Equal("test-group", repository.Owner);
        Assert.Equal("subgroup", repository.Project);
        Assert.Equal("test-project", repository.Name);
    }

    [Fact]
    public async Task CreateRepository_MapsVisibilityDescriptionAndDates()
    {
        var (provider, client) = CreateProvider();
        var request = new CreateRepositoryRequest
        {
            Owner = "test-group",
            Name = "test-project",
            Description = "Description",
            Visibility = VcsVisibility.Public,
            InitializeWithReadme = true,
            DefaultBranch = "main",
        };

        client.CreateProjectAsync(
                "test-group",
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.Project(
                visibility: VcsVisibility.Public));

        var repository = await provider.CreateRepositoryAsync(request);

        Assert.Equal("42", repository.Id);
        Assert.Equal("https://gitlab.test/test-group/test-project", repository.Url);
        Assert.Equal("main", repository.DefaultBranch);
        Assert.Equal(VcsVisibility.Public, repository.Visibility);
        Assert.Equal("Test project", repository.Description);
        Assert.Equal(DateTimeOffset.UnixEpoch, repository.CreatedAt);
    }

    [Fact]
    public async Task CreateRepository_WithMissingName_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.CreateRepositoryAsync(
            new CreateRepositoryRequest { Name = " ", Owner = "test-group" }));
    }

    [Fact]
    public async Task CreateRepository_WithoutNamespace_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.CreateRepositoryAsync(
            new CreateRepositoryRequest { Name = "test-project" }));
    }

    [Fact]
    public async Task CreateRepository_DuplicatePath_ThrowsVcsConflictException()
    {
        var (provider, client) = CreateProvider();

        client.CreateProjectAsync(
                Arg.Any<string>(),
                Arg.Any<CreateRepositoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GitLabProject>>(_ => throw new GitLabApiException(
                HttpStatusCode.BadRequest,
                "{\"message\":{\"path\":[\"has already been taken\"]}}",
                retryAfter: null));

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.CreateRepositoryAsync(CreateRepositoryRequest()));
    }

    [Fact]
    public async Task GetRepository_WithoutNamespace_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.GetRepositoryAsync(new RepositoryRef { Name = "test-project" }));
    }

    [Fact]
    public async Task GetRepository_Missing_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.GetProjectAsync("test-group/missing", Arg.Any<CancellationToken>())
            .Returns<Task<GitLabProject>>(_ => throw new GitLabApiException(
                HttpStatusCode.NotFound,
                "Not Found",
                retryAfter: null));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.GetRepositoryAsync(new RepositoryRef
            {
                Owner = "test-group",
                Name = "missing",
            }));
    }

    [Fact]
    public async Task GetRepository_WhenMarkedForDeletion_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.GetProjectAsync("test-group/test-project", Arg.Any<CancellationToken>())
            .Returns(TestData.Project(deleted: true));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.GetRepositoryAsync(new RepositoryRef
            {
                Owner = "test-group",
                Name = "test-project",
            }));
    }

    [Fact]
    public async Task GetRepository_WithSubgroup_UsesEncodedPath()
    {
        var (provider, client) = CreateProvider();
        var repository = new RepositoryRef
        {
            Owner = "test-group",
            Project = "subgroup/team",
            Name = "test-project",
        };

        client.GetProjectAsync("test-group/subgroup/team/test-project", Arg.Any<CancellationToken>())
            .Returns(TestData.Project(project: "subgroup/team"));

        var result = await provider.GetRepositoryAsync(repository);

        Assert.Equal("test-group", result.Owner);
        Assert.Equal("subgroup/team", result.Project);
        await client.Received(1).GetProjectAsync(
            "test-group/subgroup/team/test-project",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRepository_WithDefaultNamespace_UsesConfiguredNamespace()
    {
        var (provider, client) = CreateProvider(new GitLabOptions
        {
            Token = "test-token",
            DefaultNamespace = "default-group",
        });

        client.GetProjectAsync("default-group/test-project", Arg.Any<CancellationToken>())
            .Returns(TestData.Project(owner: "default-group"));

        var repository = await provider.GetRepositoryAsync(
            new RepositoryRef { Name = "test-project" });

        Assert.Equal("default-group/test-project", repository.FullName);
    }

    [Fact]
    public async Task RepositoryExists_WhenFound_ReturnsTrue()
    {
        var (provider, client) = CreateProvider();

        client.GetProjectAsync("test-group/test-project", Arg.Any<CancellationToken>())
            .Returns(TestData.Project());

        var exists = await provider.RepositoryExistsAsync(
            new RepositoryRef { Owner = "test-group", Name = "test-project" });

        Assert.True(exists);
    }

    [Fact]
    public async Task RepositoryExists_WhenMissing_ReturnsFalse()
    {
        var (provider, client) = CreateProvider();

        client.GetProjectAsync("test-group/missing", Arg.Any<CancellationToken>())
            .Returns<Task<GitLabProject>>(_ => throw new GitLabApiException(
                HttpStatusCode.NotFound,
                "Not Found",
                retryAfter: null));

        var exists = await provider.RepositoryExistsAsync(
            new RepositoryRef { Owner = "test-group", Name = "missing" });

        Assert.False(exists);
    }

    [Fact]
    public async Task RepositoryExists_WhenMarkedForDeletion_ReturnsFalse()
    {
        var (provider, client) = CreateProvider();

        client.GetProjectAsync("test-group/test-project", Arg.Any<CancellationToken>())
            .Returns(TestData.Project(deleted: true));

        var exists = await provider.RepositoryExistsAsync(
            new RepositoryRef { Owner = "test-group", Name = "test-project" });

        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteRepository_DeletesProject()
    {
        var (provider, client) = CreateProvider();

        await provider.DeleteRepositoryAsync(
            new RepositoryRef { Owner = "test-group", Name = "test-project" });

        await client.Received(1).DeleteProjectAsync(
            "test-group/test-project",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRepository_Missing_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.DeleteProjectAsync("test-group/missing", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new GitLabApiException(
                HttpStatusCode.NotFound,
                "Not Found",
                retryAfter: null)));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.DeleteRepositoryAsync(new RepositoryRef
            {
                Owner = "test-group",
                Name = "missing",
            }));
    }

    [Fact]
    public async Task GetRepository_AuthorizationFailure_ThrowsVcsAuthorizationException()
    {
        var (provider, client) = CreateProvider();

        client.GetProjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<GitLabProject>>(_ => throw new GitLabApiException(
                HttpStatusCode.Forbidden,
                "Forbidden",
                retryAfter: null));

        await Assert.ThrowsAsync<VcsAuthorizationException>(
            () => provider.GetRepositoryAsync(new RepositoryRef
            {
                Owner = "test-group",
                Name = "test-project",
            }));
    }

    [Fact]
    public async Task ListRepositories_WithoutNamespace_ListsUserProjects()
    {
        var (provider, client) = CreateProvider();

        client.ListProjectsAsync(
                null,
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.ProjectPage([TestData.Project()], totalCount: 1));

        var result = await provider.ListRepositoriesAsync();

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        await client.Received(1).ListProjectsAsync(
            null,
            Arg.Any<RepositoryQuery>(),
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRepositories_WithOwner_ListsGroupProjects()
    {
        var (provider, client) = CreateProvider();
        var query = new RepositoryQuery { Owner = "test-group" };

        client.ListProjectsAsync(
                "test-group",
                query,
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.ProjectPage([]));

        await provider.ListRepositoriesAsync(query);

        await client.Received(1).ListProjectsAsync(
            "test-group",
            query,
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRepositories_WithSubgroup_ListsNestedNamespace()
    {
        var (provider, client) = CreateProvider();
        var query = new RepositoryQuery { Owner = "test-group", Project = "subgroup" };

        client.ListProjectsAsync(
                "test-group/subgroup",
                query,
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.ProjectPage([]));

        await provider.ListRepositoriesAsync(query);

        await client.Received(1).ListProjectsAsync(
            "test-group/subgroup",
            query,
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRepositories_WithDefaultNamespace_UsesConfiguredNamespace()
    {
        var (provider, client) = CreateProvider(new GitLabOptions
        {
            Token = "test-token",
            DefaultNamespace = "default-group",
        });

        client.ListProjectsAsync(
                "default-group",
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.ProjectPage([]));

        await provider.ListRepositoriesAsync();

        await client.Received(1).ListProjectsAsync(
            "default-group",
            Arg.Any<RepositoryQuery>(),
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRepositories_MapsPaginationMetadata()
    {
        var (provider, client) = CreateProvider();
        var pagination = new PaginationOptions { Page = 2, PageSize = 5 };

        client.ListProjectsAsync(
                null,
                Arg.Any<RepositoryQuery>(),
                pagination,
                Arg.Any<CancellationToken>())
            .Returns(TestData.ProjectPage(
                [TestData.Project()],
                hasNextPage: true,
                totalCount: 11));

        var result = await provider.ListRepositoriesAsync(pagination: pagination);

        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.True(result.HasNextPage);
        Assert.Equal(11, result.TotalCount);
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

        client.ListProjectsAsync(
                null,
                query,
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.ProjectPage(
            [
                TestData.Project(name: "alpha"),
                TestData.Project(name: "beta"),
                TestData.Project(name: "alpha-archived", archived: true),
                TestData.Project(name: "alpha-public", visibility: VcsVisibility.Public),
            ]));

        var result = await provider.ListRepositoriesAsync(query);

        var repository = Assert.Single(result.Items);
        Assert.Equal("alpha", repository.Name);
    }

    [Fact]
    public async Task ListRepositories_IncludeArchived_ReturnsArchivedProjects()
    {
        var (provider, client) = CreateProvider();
        var query = new RepositoryQuery { IncludeArchived = true };

        client.ListProjectsAsync(
                null,
                query,
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.ProjectPage(
            [
                TestData.Project(name: "active"),
                TestData.Project(name: "archived", archived: true),
            ]));

        var result = await provider.ListRepositoriesAsync(query);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListRepositories_AuthorizationFailure_ThrowsVcsAuthorizationException()
    {
        var (provider, client) = CreateProvider();

        client.ListProjectsAsync(
                Arg.Any<string?>(),
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GitLabPage<GitLabProject>>>(_ => throw new GitLabApiException(
                HttpStatusCode.Forbidden,
                "Forbidden",
                retryAfter: null));

        await Assert.ThrowsAsync<VcsAuthorizationException>(
            () => provider.ListRepositoriesAsync());
    }

    private static CreateRepositoryRequest CreateRepositoryRequest()
    {
        return new CreateRepositoryRequest
        {
            Owner = "test-group",
            Name = "test-project",
        };
    }

    [Fact]
    public async Task ListRepositories_PaddedNameContains_IsNormalized()
    {
        var (provider, client) = CreateProvider();

        client.ListProjectsAsync(
                null,
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.ProjectPage(
            [
                TestData.Project(name: "alpha"),
                TestData.Project(name: "beta"),
            ]));

        var result = await provider.ListRepositoriesAsync(
            new RepositoryQuery { NameContains = " alpha " });

        var project = Assert.Single(result.Items);
        Assert.Equal("alpha", project.Name);
        await client.Received(1).ListProjectsAsync(
            null,
            Arg.Is<RepositoryQuery>(query => query.NameContains == "alpha"),
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    private static (GitLabProvider Provider, IGitLabApiClient Client) CreateProvider(
        GitLabOptions? options = null)
    {
        var client = Substitute.For<IGitLabApiClient>();
        return (
            new GitLabProvider(client, options ?? new GitLabOptions { Token = "test-token" }),
            client);
    }
}
