using System.Net;
using NSubstitute;
using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps.Tests;

public sealed class AzureDevOpsRepositoryListingTests
{
    [Fact]
    public async Task ListRepositories_WithProject_ListsProjectRepositories()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                TestData.Project,
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage([TestData.Repository()]));

        var result = await provider.ListRepositoriesAsync(
            new RepositoryQuery { Project = TestData.Project });

        Assert.Single(result.Items);
        Assert.Equal("test-organization/test-project/test-repository", result.Items[0].FullName);
    }

    [Fact]
    public async Task ListRepositories_WithDefaultProject_UsesConfiguredProject()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                TestData.Project,
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage([TestData.Repository()]));

        await provider.ListRepositoriesAsync();

        await client.Received(1).ListRepositoriesAsync(
            TestData.Project,
            Arg.Any<RepositoryQuery>(),
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRepositories_WithoutProject_UsesOrganizationScope()
    {
        var (provider, client) = CreateProvider(defaultProject: null);

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
    public async Task ListRepositories_ReportsPaginationMetadata()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                Arg.Any<string?>(),
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage(
                [TestData.Repository("one"), TestData.Repository("two")],
                hasNextPage: true,
                totalCount: 12));

        var result = await provider.ListRepositoriesAsync(
            new RepositoryQuery { NameContains = "test" },
            new PaginationOptions { Page = 2, PageSize = 2 });

        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.HasNextPage);
        Assert.Equal(12, result.TotalCount);
    }

    [Fact]
    public async Task ListRepositories_FiltersByNameOnThePage()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                Arg.Any<string?>(),
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage(
            [
                TestData.Repository("alpha-service"),
                TestData.Repository("beta-service"),
                TestData.Repository("alpha-web"),
            ]));

        var result = await provider.ListRepositoriesAsync(
            new RepositoryQuery { NameContains = "alpha" });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.StartsWith("alpha", item.Name));
    }

    [Fact]
    public async Task ListRepositories_WithNonPrivateVisibilityFilter_ReturnsNoRepositories()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                Arg.Any<string?>(),
                Arg.Any<RepositoryQuery>(),
                Arg.Any<PaginationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(TestData.RepositoryPage([TestData.Repository()]));

        var result = await provider.ListRepositoriesAsync(
            new RepositoryQuery { Visibility = VcsVisibility.Public });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ListRepositories_WithMismatchedOwner_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.ListRepositoriesAsync(
                new RepositoryQuery { Owner = "other-organization" }));
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
            .Returns<Task<AzureDevOpsPage<AzureDevOpsRepository>>>(_ =>
                throw AzureDevOpsRepositoryTests.CreateResponseException(
                    HttpStatusCode.InternalServerError,
                    "Service unavailable"));

        await Assert.ThrowsAsync<VcsProviderException>(
            () => provider.ListRepositoriesAsync());
    }

    [Fact]
    public async Task ListRepositories_PaddedNameContains_IsNormalized()
    {
        var (provider, client) = CreateProvider();

        client.ListRepositoriesAsync(
                Arg.Any<string?>(),
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
            Arg.Any<string?>(),
            Arg.Is<RepositoryQuery>(query => query.NameContains == "alpha"),
            Arg.Any<PaginationOptions>(),
            Arg.Any<CancellationToken>());
    }

    private static (AzureDevOpsProvider Provider, IAzureDevOpsApiClient Client) CreateProvider(
        string? defaultProject = TestData.Project)
    {
        return AzureDevOpsRepositoryTests.CreateProvider(defaultProject);
    }
}
