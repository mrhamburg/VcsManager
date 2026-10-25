using System.Net;
using NSubstitute;
using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaRepositoryTests
{
    [Fact]
    public async Task CreateRepository_ForUserOwner_PassesOwnerAndRequest()
    {
        var (provider, client) = CreateProvider();
        var request = new CreateRepositoryRequest
        {
            Owner = TestData.Owner,
            Name = TestData.RepositoryName,
            Description = "Test repository",
            Visibility = VcsVisibility.Public,
            InitializeWithReadme = true,
            DefaultBranch = "main",
        };

        client.CreateRepositoryAsync(TestData.Owner, request, Arg.Any<CancellationToken>())
            .Returns(TestData.Repository());

        var repository = await provider.CreateRepositoryAsync(request);

        Assert.Equal("42", repository.Id);
        Assert.Equal("test-owner/test-repository", repository.FullName);
        Assert.Equal("test-owner", repository.Owner);
        Assert.Null(repository.Project);
        Assert.Equal("main", repository.DefaultBranch);
        await client.Received(1).CreateRepositoryAsync(
            TestData.Owner,
            Arg.Is<CreateRepositoryRequest>(value =>
                value.InitializeWithReadme),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRepository_WithDefaultOwner_UsesConfiguredOwner()
    {
        var (provider, client) = CreateProvider();
        var request = new CreateRepositoryRequest { Name = TestData.RepositoryName };

        client.CreateRepositoryAsync(TestData.Owner, request, Arg.Any<CancellationToken>())
            .Returns(TestData.Repository());

        var repository = await provider.CreateRepositoryAsync(request);

        Assert.Equal(TestData.Owner, repository.Owner);
    }

    [Fact]
    public async Task CreateRepository_WithoutOwner_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider(defaultOwner: null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.CreateRepositoryAsync(
                new CreateRepositoryRequest { Name = TestData.RepositoryName }));

        Assert.Contains("owner", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRepository_WithProject_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.CreateRepositoryAsync(new CreateRepositoryRequest
            {
                Owner = TestData.Owner,
                Project = "subgroup",
                Name = TestData.RepositoryName,
            }));
    }

    [Fact]
    public async Task CreateRepository_WithInternalVisibility_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.CreateRepositoryAsync(new CreateRepositoryRequest
            {
                Owner = TestData.Owner,
                Name = TestData.RepositoryName,
                Visibility = VcsVisibility.Internal,
            }));

        Assert.Contains("visibility", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRepository_WithMissingName_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.CreateRepositoryAsync(new CreateRepositoryRequest
            {
                Owner = TestData.Owner,
                Name = " ",
            }));
    }

    [Fact]
    public async Task CreateRepository_Duplicate_ThrowsVcsConflictException()
    {
        var (provider, client) = CreateProvider();

        client.CreateRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<CreateRepositoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GiteaRepository>>(_ => throw TestData.ApiException(
                HttpStatusCode.Conflict,
                "{\"message\":\"repository already exists\"}"));

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.CreateRepositoryAsync(new CreateRepositoryRequest
            {
                Owner = TestData.Owner,
                Name = TestData.RepositoryName,
            }));
    }

    [Fact]
    public async Task GetRepository_MapsRepository()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(TestData.Owner, TestData.RepositoryName, Arg.Any<CancellationToken>())
            .Returns(TestData.Repository(visibility: VcsVisibility.Public, archived: true));

        var repository = await provider.GetRepositoryAsync(
            new RepositoryRef { Name = TestData.RepositoryName });

        Assert.Equal(VcsVisibility.Public, repository.Visibility);
        Assert.True(repository.IsArchived);
    }

    [Fact]
    public async Task GetRepository_Missing_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GiteaRepository>>(_ => throw TestData.ApiException(HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.GetRepositoryAsync(
                new RepositoryRef { Owner = TestData.Owner, Name = "missing" }));
    }

    [Fact]
    public async Task GetRepository_WithProject_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.GetRepositoryAsync(new RepositoryRef
            {
                Owner = TestData.Owner,
                Project = "subgroup",
                Name = TestData.RepositoryName,
            }));
    }

    [Fact]
    public async Task RepositoryExists_WhenFound_ReturnsTrue()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                TestData.Owner,
                TestData.RepositoryName,
                Arg.Any<CancellationToken>())
            .Returns(TestData.Repository());

        Assert.True(await provider.RepositoryExistsAsync(
            new RepositoryRef { Name = TestData.RepositoryName }));
    }

    [Fact]
    public async Task RepositoryExists_WhenMissing_ReturnsFalse()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GiteaRepository>>(_ => throw TestData.ApiException(HttpStatusCode.NotFound));

        Assert.False(await provider.RepositoryExistsAsync(
            new RepositoryRef { Name = "missing" }));
    }

    [Fact]
    public async Task DeleteRepository_DeletesRepository()
    {
        var (provider, client) = CreateProvider();

        await provider.DeleteRepositoryAsync(
            new RepositoryRef { Owner = TestData.Owner, Name = TestData.RepositoryName });

        await client.Received(1).DeleteRepositoryAsync(
            TestData.Owner,
            TestData.RepositoryName,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRepository_Missing_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.DeleteRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw TestData.ApiException(HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.DeleteRepositoryAsync(
                new RepositoryRef { Owner = TestData.Owner, Name = "missing" }));
    }

    [Fact]
    public async Task RepositoryOperation_WhenUnauthorized_ThrowsAuthenticationException()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GiteaRepository>>(_ => throw TestData.ApiException(
                HttpStatusCode.Unauthorized));

        await Assert.ThrowsAsync<VcsAuthenticationException>(
            () => provider.GetRepositoryAsync(
                new RepositoryRef { Owner = TestData.Owner, Name = TestData.RepositoryName }));
    }

    [Fact]
    public async Task RepositoryOperation_WhenForbidden_ThrowsAuthorizationException()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GiteaRepository>>(_ => throw TestData.ApiException(
                HttpStatusCode.Forbidden));

        await Assert.ThrowsAsync<VcsAuthorizationException>(
            () => provider.GetRepositoryAsync(
                new RepositoryRef { Owner = TestData.Owner, Name = TestData.RepositoryName }));
    }

    [Fact]
    public async Task RepositoryOperation_WhenRateLimited_ThrowsRateLimitException()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GiteaRepository>>(_ => throw TestData.ApiException(
                HttpStatusCode.TooManyRequests,
                retryAfter: TimeSpan.FromSeconds(30)));

        var exception = await Assert.ThrowsAsync<VcsRateLimitException>(
            () => provider.GetRepositoryAsync(
                new RepositoryRef { Owner = TestData.Owner, Name = TestData.RepositoryName }));

        Assert.Equal(TimeSpan.FromSeconds(30), exception.RetryAfter);
    }

    [Fact]
    public async Task DisposeAsync_DisposesClient()
    {
        var (provider, client) = CreateProvider();

        await provider.DisposeAsync();

        await client.Received(1).DisposeAsync();
    }

    internal static (GiteaProvider Provider, IGiteaApiClient Client) CreateProvider(
        string? defaultOwner = TestData.Owner)
    {
        var options = new GiteaOptions
        {
            BaseUrl = TestData.BaseUrl,
            Token = "test-token",
            DefaultOwner = defaultOwner,
        };

        var client = Substitute.For<IGiteaApiClient>();
        return (new GiteaProvider(client, options), client);
    }
}
