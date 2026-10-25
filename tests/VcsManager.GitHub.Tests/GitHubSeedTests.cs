using System.Net;
using Octokit;
using VcsManager.GitHub.Internal;

namespace VcsManager.GitHub.Tests;

public sealed class GitHubSeedTests
{
    [Fact]
    public async Task SeedRepository_PassesNormalizedRequestAndSetsDefaultBranch()
    {
        var (provider, client) = CreateProvider();
        var repository = new RepositoryRef { Owner = "test-owner", Name = "test-repo" };
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
        var repository = new RepositoryRef { Owner = "test-owner", Name = "test-repo" };
        await provider.CreateRepositoryAsync(new CreateRepositoryRequest
        {
            Owner = repository.Owner,
            Name = repository.Name,
        });
        client.SeedFailure = new ApiException("Forbidden", HttpStatusCode.Forbidden);

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

    private static (GitHubProvider Provider, FakeGitHubApiClient Client) CreateProvider()
    {
        var client = new FakeGitHubApiClient();
        return (new GitHubProvider(client, new GitHubOptions { Token = "test-token" }), client);
    }
}
