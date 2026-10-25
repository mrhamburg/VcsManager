using System.Net;
using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps.Tests;

public sealed class AzureDevOpsSeedTests
{
    [Fact]
    public async Task SeedRepository_PassesNormalizedRequestAndSetsDefaultBranch()
    {
        var (provider, client) = CreateProvider();
        var repository = new RepositoryRef
        {
            Owner = FakeAzureDevOpsApiClient.Organization,
            Project = TestData.Project,
            Name = "test-repository",
        };
        await provider.CreateRepositoryAsync(new CreateRepositoryRequest
        {
            Project = repository.Project,
            Name = repository.Name,
        });

        await provider.SeedRepositoryAsync(repository, new SeedRepositoryRequest
        {
            Branch = " seeded ",
            Files =
            [
                new RepositorySeedFile
                {
                    Path = "README.md",
                    Content = "hello"u8.ToArray(),
                },
            ],
        });

        Assert.NotNull(client.LastSeedRequest);
        Assert.Equal("seeded", client.LastSeedRequest.Branch);
        Assert.Equal("Initial commit", client.LastSeedRequest.CommitMessage);

        var seeded = await provider.GetRepositoryAsync(repository);
        Assert.Equal("seeded", seeded.DefaultBranch);
    }

    [Fact]
    public async Task SeedRepository_WhenForbidden_ThrowsAuthorizationException()
    {
        var (provider, client) = CreateProvider();
        var repository = new RepositoryRef
        {
            Owner = FakeAzureDevOpsApiClient.Organization,
            Project = TestData.Project,
            Name = "test-repository",
        };
        await provider.CreateRepositoryAsync(new CreateRepositoryRequest
        {
            Project = repository.Project,
            Name = repository.Name,
        });
        client.SeedFailure = new AzureDevOpsApiException(
            HttpStatusCode.Forbidden,
            "Forbidden",
            null);

        await Assert.ThrowsAsync<VcsAuthorizationException>(
            () => provider.SeedRepositoryAsync(repository, SeedRequest()));
    }

    private static SeedRepositoryRequest SeedRequest()
    {
        return new SeedRepositoryRequest
        {
            Branch = "main",
            Files =
            [
                new RepositorySeedFile
                {
                    Path = "README.md",
                    Content = "hello"u8.ToArray(),
                },
            ],
        };
    }

    private static (AzureDevOpsProvider Provider, FakeAzureDevOpsApiClient Client) CreateProvider()
    {
        var client = new FakeAzureDevOpsApiClient();
        return (new AzureDevOpsProvider(client, new AzureDevOpsOptions
        {
            OrganizationUrl = $"https://dev.azure.com/{FakeAzureDevOpsApiClient.Organization}",
            PersonalAccessToken = "test-token",
            DefaultProject = TestData.Project,
        }), client);
    }
}
