using System.Net.Http.Json;
using System.Text;
using VcsManager.Gitea;
using Xunit;

namespace VcsManager.IntegrationTests;

public sealed class GiteaIntegrationTests
{
    [GiteaFact]
    public async Task FullFlow_CreatesListsMergesClosesAndDeletesRepository()
    {
        var configuration = GiteaTestConfiguration.FromEnvironment();
        using var httpClient = CreateHttpClient(configuration);
        var owner = configuration.Organization ?? await GetAuthenticatedUserAsync(httpClient);

        await using var provider = new GiteaProvider(new GiteaOptions
        {
            BaseUrl = configuration.BaseUrl,
            Token = configuration.Token,
            DefaultOwner = owner,
        });

        var repositoryName = CreateRepositoryName();
        RepositoryRef? repository = null;

        try
        {
            var created = await provider.CreateRepositoryAsync(new CreateRepositoryRequest
            {
                Name = repositoryName,
                Visibility = VcsVisibility.Private,
                InitializeWithReadme = true,
                DefaultBranch = "main",
            });

            repository = new RepositoryRef { Owner = owner, Name = repositoryName };
            Console.WriteLine($"Created repository: {created.FullName}");

            await CreateBranchWithCommitAsync(
                httpClient,
                owner,
                repositoryName,
                branch: "sample/merge",
                fileName: "merge.txt");
            await CreateBranchWithCommitAsync(
                httpClient,
                owner,
                repositoryName,
                branch: "sample/close",
                fileName: "close.txt");

            var repositories = await provider.ListRepositoriesAsync(
                new RepositoryQuery { NameContains = repositoryName },
                new PaginationOptions { PageSize = 10 });

            Assert.Contains(repositories.Items, item => item.Name == repositoryName);
            Console.WriteLine($"Listed repositories: {repositories.Items.Count}");

            var first = await provider.CreatePullRequestAsync(
                repository,
                new CreatePullRequestRequest
                {
                    Title = "Integration test: merge",
                    SourceBranch = "sample/merge",
                    TargetBranch = "main",
                });
            Console.WriteLine($"Created pull request #{first.Number}: {first.State}");

            var merged = await provider.MergePullRequestAsync(
                repository,
                first.Number,
                new MergePullRequestRequest
                {
                    Method = MergeMethod.Squash,
                    DeleteSourceBranch = true,
                });

            Assert.Equal(PullRequestState.Merged, merged.State);
            Console.WriteLine($"Merged pull request #{merged.Number}: {merged.State}");

            var second = await provider.CreatePullRequestAsync(
                repository,
                new CreatePullRequestRequest
                {
                    Title = "Integration test: close",
                    SourceBranch = "sample/close",
                    TargetBranch = "main",
                });
            Console.WriteLine($"Created pull request #{second.Number}: {second.State}");

            var closed = await provider.ClosePullRequestAsync(repository, second.Number);

            Assert.Equal(PullRequestState.Closed, closed.State);
            Console.WriteLine($"Closed pull request #{closed.Number}: {closed.State}");
        }
        finally
        {
            if (repository is not null)
            {
                try
                {
                    await provider.DeleteRepositoryAsync(repository);
                    Console.WriteLine($"Deleted repository: {repository.Owner}/{repository.Name}");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(
                        $"Cleanup failed for {repository.Owner}/{repository.Name}: {exception.Message}");
                }
            }
        }
    }

    private static HttpClient CreateHttpClient(GiteaTestConfiguration configuration)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(configuration.BaseUrl.TrimEnd('/') + "/api/v1/"),
        };

        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "Authorization",
            $"token {configuration.Token}");

        return client;
    }

    private static async Task<string> GetAuthenticatedUserAsync(HttpClient client)
    {
        using var response = await client.GetAsync("user");
        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<AuthenticatedUser>();

        return user?.Login ?? throw new InvalidOperationException(
            "Gitea did not return the authenticated user.");
    }

    private static async Task CreateBranchWithCommitAsync(
        HttpClient client,
        string owner,
        string repositoryName,
        string branch,
        string fileName)
    {
        var path =
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repositoryName)}" +
            $"/contents/{Uri.EscapeDataString(fileName)}";

        var body = new
        {
            content = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"VcsManager integration test for {branch} at {DateTimeOffset.UtcNow:O}.")),
            message = $"Add {fileName}",
            branch = "main",
            new_branch = branch,
        };

        using var response = await client.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
    }

    private static string CreateRepositoryName()
    {
        return $"vcsmanager-it-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..48];
    }

    private sealed record AuthenticatedUser(string Login);
}
