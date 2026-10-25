using System.Net;
using VcsManager.GitLab.Internal;

namespace VcsManager.GitLab.Tests;

public sealed class GitLabSeedTests
{
    [Fact]
    public async Task SeedRepository_PassesNormalizedRequestAndSetsDefaultBranch()
    {
        var (provider, client) = CreateProvider();
        var repository = new RepositoryRef { Owner = "test-group", Name = "test-project" };
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
        var repository = new RepositoryRef { Owner = "test-group", Name = "test-project" };
        await provider.CreateRepositoryAsync(new CreateRepositoryRequest
        {
            Owner = repository.Owner,
            Name = repository.Name,
        });
        client.SeedFailure = new GitLabApiException(HttpStatusCode.Forbidden, "Forbidden", null);

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

    private static (GitLabProvider Provider, FakeGitLabApiClient Client) CreateProvider()
    {
        var client = new FakeGitLabApiClient();
        return (new GitLabProvider(client, new GitLabOptions
        {
            Token = "test-token",
            DefaultNamespace = "test-group",
        }), client);
    }
}
