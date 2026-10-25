using Octokit.Internal;
using VcsManager.GitHub.Internal;

namespace VcsManager.GitHub.Tests;

public sealed class OctokitGitHubApiClientTests
{
    private static readonly SimpleJsonSerializer Serializer = new();

    [Fact]
    public void SameRepositoryInForkedRepository_IsNotFromFork()
    {
        var pullRequest = Deserialize(
            """
            {
              "head": { "ref": "feature", "repo": { "full_name": "owner/repository", "fork": true } },
              "base": { "ref": "main", "repo": { "full_name": "owner/repository", "fork": false } }
            }
            """);

        Assert.False(OctokitGitHubApiClient.IsFromFork(pullRequest));
    }

    [Fact]
    public void DifferentRepositories_IsFromFork()
    {
        var pullRequest = Deserialize(
            """
            {
              "head": { "ref": "feature", "repo": { "full_name": "fork-owner/repository", "fork": true } },
              "base": { "ref": "main", "repo": { "full_name": "owner/repository", "fork": false } }
            }
            """);

        Assert.True(OctokitGitHubApiClient.IsFromFork(pullRequest));
    }

    [Fact]
    public void MissingHeadRepository_IsFromFork()
    {
        var pullRequest = Deserialize(
            """
            {
              "head": { "ref": "feature" },
              "base": { "ref": "main", "repo": { "full_name": "owner/repository", "fork": false } }
            }
            """);

        Assert.True(OctokitGitHubApiClient.IsFromFork(pullRequest));
    }

    [Fact]
    public void MissingBaseRepository_UsesHeadForkFlag()
    {
        var pullRequest = Deserialize(
            """
            {
              "head": { "ref": "feature", "repo": { "full_name": "owner/repository", "fork": true } }
            }
            """);

        Assert.True(OctokitGitHubApiClient.IsFromFork(pullRequest));
    }

    private static Octokit.PullRequest Deserialize(string json)
    {
        return Serializer.Deserialize<Octokit.PullRequest>(json);
    }
}
