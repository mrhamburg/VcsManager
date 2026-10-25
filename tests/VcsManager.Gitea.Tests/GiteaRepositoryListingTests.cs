using NSubstitute;
using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaRepositoryListingTests
{
    [Fact]
    public async Task ListRepositories_WithOwner_PassesOwner()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                TestData.Owner,
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage([TestData.Repository()]));

        var result = await provider.ListRepositoriesAsync(
            new RepositoryQuery { Owner = TestData.Owner });

        Assert.Single(result.Items);
        Assert.Equal("test-owner/test-repository", result.Items[0].FullName);
    }

    [Fact]
    public async Task ListRepositories_WithDefaultOwner_UsesConfiguredOwner()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                TestData.Owner,
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage([TestData.Repository()]));

        await provider.ListRepositoriesAsync();

        await client.Received(1).ListRepositoriesAsync(
            TestData.Owner,
            Arg.Any<RepositoryQuery>(),
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRepositories_WithoutOwner_UsesUnscopedListing()
    {
        var (provider, client) = CreateProvider(defaultOwner: null);

        client.ListRepositoriesAsync(
                null,
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage([TestData.Repository()]));

        var result = await provider.ListRepositoriesAsync();

        Assert.Single(result.Items);
        await client.Received(1).ListRepositoriesAsync(
            null,
            Arg.Any<RepositoryQuery>(),
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRepositories_ReportsPaginationMetadataAndPageSizeCap()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                Arg.Any<string?>(),
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage(
                [TestData.Repository()],
                hasNextPage: true,
                totalCount: 120));

        var result = await provider.ListRepositoriesAsync(
            pagination: new PaginationOptions { Page = 2, PageSize = 100 });

        Assert.Equal(2, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.True(result.HasNextPage);
        Assert.Equal(120, result.TotalCount);
    }

    [Fact]
    public async Task ListRepositories_ExcludesArchivedByDefaultAndIncludesOnRequest()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                Arg.Any<string?>(),
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage(
            [
                TestData.Repository(name: "active"),
                TestData.Repository(name: "archived", archived: true),
            ]));

        var withoutArchived = await provider.ListRepositoriesAsync();
        var withArchived = await provider.ListRepositoriesAsync(
            new RepositoryQuery { IncludeArchived = true });

        Assert.Single(withoutArchived.Items);
        Assert.Equal("active", withoutArchived.Items[0].Name);
        Assert.Equal(2, withArchived.Items.Count);
    }

    [Fact]
    public async Task ListRepositories_FiltersByNameAndVisibility()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                Arg.Any<string?>(),
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage(
            [
                TestData.Repository(name: "alpha-one", visibility: VcsVisibility.Private),
                TestData.Repository(name: "alpha-two", visibility: VcsVisibility.Public),
                TestData.Repository(name: "beta-one", visibility: VcsVisibility.Private),
            ]));

        var result = await provider.ListRepositoriesAsync(
            new RepositoryQuery { NameContains = "alpha", Visibility = VcsVisibility.Public });

        Assert.Single(result.Items);
        Assert.Equal("alpha-two", result.Items[0].Name);
    }

    [Fact]
    public async Task ListRepositories_WithInternalVisibility_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.ListRepositoriesAsync(
                new RepositoryQuery { Visibility = VcsVisibility.Internal }));

        Assert.Contains("visibility", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListRepositories_WithProject_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.ListRepositoriesAsync(
                new RepositoryQuery { Project = "subgroup" }));
    }

    [Fact]
    public async Task ListRepositories_WhenClientThrows_MapsProviderException()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                Arg.Any<string?>(),
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<GiteaPage<GiteaRepository>>>(_ =>
                throw TestData.ApiException(System.Net.HttpStatusCode.Forbidden));

        await Assert.ThrowsAsync<VcsAuthorizationException>(
            () => provider.ListRepositoriesAsync());
    }

    private static (GiteaProvider Provider, IGiteaApiClient Client) CreateProvider(
        string? defaultOwner = TestData.Owner)
    {
        return GiteaRepositoryTests.CreateProvider(defaultOwner);
    }
}
