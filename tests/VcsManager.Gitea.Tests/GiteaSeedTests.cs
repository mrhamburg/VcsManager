using System.Net;
using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaSeedTests
{
    [Fact]
    public async Task SeedRepository_PassesNormalizedRequestAndSetsDefaultBranch()
    {
        var (provider, client) = CreateProvider();
        var repository = new RepositoryRef { Owner = "test-owner", Name = "test-repository" };
        await provider.CreateRepositoryAsync(new CreateRepositoryRequest
        {
            Owner = repository.Owner,
            Name = repository.Name,
            DefaultBranch = "main",
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
        var repository = new RepositoryRef { Owner = "test-owner", Name = "test-repository" };
        await provider.CreateRepositoryAsync(new CreateRepositoryRequest
        {
            Owner = repository.Owner,
            Name = repository.Name,
        });
        client.SeedFailure = new GiteaApiException(HttpStatusCode.Forbidden, "Forbidden", null);

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

    private static (GiteaProvider Provider, FakeGiteaApiClient Client) CreateProvider()
    {
        var client = new FakeGiteaApiClient();
        return (new GiteaProvider(client, new GiteaOptions
        {
            BaseUrl = "https://gitea.example.com",
            Token = "test-token",
            DefaultOwner = "test-owner",
        }), client);
    }
}
