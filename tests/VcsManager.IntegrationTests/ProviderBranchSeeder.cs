using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VcsManager.Builder;

namespace VcsManager.IntegrationTests;

internal sealed class ProviderBranchSeeder : IDisposable
{
    private readonly VcsProviderConfiguration _configuration;
    private readonly VcsRepository _repository;
    private readonly string _providerName;
    private readonly HttpClient _httpClient;

    public ProviderBranchSeeder(VcsProviderConfiguration configuration, VcsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(repository);

        _configuration = configuration;
        _repository = repository;
        _providerName = configuration.ProviderName.ToLowerInvariant();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VcsManager.IntegrationTests/1.0");

        ConfigureClient();
    }

    public Task SeedAsync(string branchName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(branchName);

        return _providerName switch
        {
            "gitea" => SeedGiteaAsync(branchName, cancellationToken),
            "github" => SeedGitHubAsync(branchName, cancellationToken),
            "gitlab" => SeedGitLabAsync(branchName, cancellationToken),
            "azuredevops" => SeedAzureDevOpsAsync(branchName, cancellationToken),
            _ => throw new InvalidOperationException(
                $"Branch seeding is not implemented for provider '{_configuration.ProviderName}'."),
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private void ConfigureClient()
    {
        switch (_providerName)
        {
            case "gitea":
                _httpClient.BaseAddress = new Uri(
                    GetRequired("base.url").TrimEnd('/') + "/api/v1/");
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                    "Authorization",
                    $"token {GetRequired("token")}");
                break;

            case "github":
                var githubBaseUrl = (GetOptional("base.url") ?? "https://api.github.com").TrimEnd('/');
                _httpClient.BaseAddress = new Uri(githubBaseUrl + "/");
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", GetRequired("token"));
                _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
                break;

            case "gitlab":
                var gitlabBaseUrl = (GetOptional("base.url") ?? "https://gitlab.com").TrimEnd('/');
                if (!gitlabBaseUrl.EndsWith("/api/v4", StringComparison.OrdinalIgnoreCase))
                {
                    gitlabBaseUrl += "/api/v4";
                }

                _httpClient.BaseAddress = new Uri(gitlabBaseUrl + "/");
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                    "PRIVATE-TOKEN",
                    GetRequired("token"));
                break;

            case "azuredevops":
                _httpClient.BaseAddress = new Uri(GetRequired("organization.url").TrimEnd('/') + "/");
                var credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($":{GetRequired("personal.access.token")}"));
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", credentials);
                break;

            default:
                throw new InvalidOperationException(
                    $"Branch seeding is not implemented for provider '{_configuration.ProviderName}'.");
        }
    }

    private async Task SeedGiteaAsync(string branchName, CancellationToken cancellationToken)
    {
        var owner = RequireOwner();
        var fileName = FileNameFor(branchName);
        var path =
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(_repository.Name)}" +
            $"/contents/{Uri.EscapeDataString(fileName)}";

        var body = new
        {
            content = ToBase64(branchName),
            message = $"Add {fileName}",
            branch = DefaultBranch,
            new_branch = branchName,
        };

        using var content = JsonContent.Create(body);
        using var response = await _httpClient.PostAsync(path, content, cancellationToken);
        await EnsureSuccessAsync(response, "Create Gitea branch", cancellationToken);
    }

    private async Task SeedGitHubAsync(string branchName, CancellationToken cancellationToken)
    {
        var owner = RequireOwner();
        var repositoryName = Uri.EscapeDataString(_repository.Name);
        var basePath = $"repos/{Uri.EscapeDataString(owner)}/{repositoryName}";

        var referencePath = $"{basePath}/git/ref/heads/{Uri.EscapeDataString(DefaultBranch)}";
        using var referenceResponse = await _httpClient.GetAsync(referencePath, cancellationToken);
        await EnsureSuccessAsync(referenceResponse, "Read GitHub default branch", cancellationToken);

        using var referenceDocument = JsonDocument.Parse(
            await referenceResponse.Content.ReadAsStringAsync(cancellationToken));
        var baseSha = referenceDocument.RootElement
            .GetProperty("object")
            .GetProperty("sha")
            .GetString()
            ?? throw new InvalidOperationException("GitHub did not return the default branch commit.");

        var branchBody = new
        {
            @ref = $"refs/heads/{branchName}",
            sha = baseSha,
        };

        using (var branchContent = JsonContent.Create(branchBody))
        using (var branchResponse = await _httpClient.PostAsync(
            $"{basePath}/git/refs",
            branchContent,
            cancellationToken))
        {
            await EnsureSuccessAsync(branchResponse, "Create GitHub branch", cancellationToken);
        }

        var fileName = FileNameFor(branchName);
        var fileBody = new
        {
            message = $"Add {fileName}",
            content = ToBase64(branchName),
            branch = branchName,
        };

        using var fileContent = JsonContent.Create(fileBody);
        using var fileResponse = await _httpClient.PutAsync(
            $"{basePath}/contents/{Uri.EscapeDataString(fileName)}",
            fileContent,
            cancellationToken);

        await EnsureSuccessAsync(fileResponse, "Commit GitHub file", cancellationToken);
    }

    private async Task SeedGitLabAsync(string branchName, CancellationToken cancellationToken)
    {
        var project = Uri.EscapeDataString(_repository.FullName);
        var fileName = FileNameFor(branchName);
        var path = $"projects/{project}/repository/files/{Uri.EscapeDataString(fileName)}";

        var body = new
        {
            branch = branchName,
            start_branch = DefaultBranch,
            content = ContentFor(branchName),
            commit_message = $"Add {fileName}",
        };

        using var content = JsonContent.Create(body);
        using var response = await _httpClient.PostAsync(path, content, cancellationToken);
        await EnsureSuccessAsync(response, "Create GitLab branch", cancellationToken);
    }

    private async Task SeedAzureDevOpsAsync(string branchName, CancellationToken cancellationToken)
    {
        var project = Uri.EscapeDataString(_repository.Project ?? GetRequired("default.project"));
        var repositoryId = Uri.EscapeDataString(_repository.Id);
        var basePath = $"{project}/_apis/git/repositories/{repositoryId}";
        var baseSha = await GetAzureRefAsync(basePath, DefaultBranch, cancellationToken);

        if (baseSha is null)
        {
            baseSha = await CreateAzureInitialCommitAsync(basePath, cancellationToken);
        }

        var fileName = FileNameFor(branchName);
        var body = new
        {
            refUpdates = new[]
            {
                new
                {
                    name = $"refs/heads/{branchName}",
                    oldObjectId = baseSha,
                },
            },
            commits = new[]
            {
                new
                {
                    comment = $"Add {fileName}",
                    changes = new[]
                    {
                        new
                        {
                            changeType = "add",
                            item = new { path = $"/{fileName}" },
                            newContent = new
                            {
                                content = ContentFor(branchName),
                                contentType = "rawText",
                            },
                        },
                    },
                },
            },
        };

        using var content = JsonContent.Create(body);
        using var response = await _httpClient.PostAsync(
            $"{basePath}/pushes?api-version=7.1",
            content,
            cancellationToken);

        await EnsureSuccessAsync(response, "Create Azure DevOps branch", cancellationToken);
    }

    private async Task<string?> GetAzureRefAsync(
        string basePath,
        string branchName,
        CancellationToken cancellationToken)
    {
        var path = $"{basePath}/refs?filter=heads/{Uri.EscapeDataString(branchName)}&api-version=7.1";
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, "Read Azure DevOps refs", cancellationToken);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));

        if (!document.RootElement.TryGetProperty("value", out var value) || value.GetArrayLength() == 0)
        {
            return null;
        }

        return value[0].GetProperty("objectId").GetString();
    }

    private async Task<string> CreateAzureInitialCommitAsync(
        string basePath,
        CancellationToken cancellationToken)
    {
        var body = new
        {
            refUpdates = new[]
            {
                new
                {
                    name = $"refs/heads/{DefaultBranch}",
                    oldObjectId = new string('0', 40),
                },
            },
            commits = new[]
            {
                new
                {
                    comment = "Initial commit",
                    changes = new[]
                    {
                        new
                        {
                            changeType = "add",
                            item = new { path = "/README.md" },
                            newContent = new
                            {
                                content = "VcsManager integration test repository.",
                                contentType = "rawText",
                            },
                        },
                    },
                },
            },
        };

        using var content = JsonContent.Create(body);
        using var response = await _httpClient.PostAsync(
            $"{basePath}/pushes?api-version=7.1",
            content,
            cancellationToken);

        await EnsureSuccessAsync(response, "Create Azure DevOps initial commit", cancellationToken);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));

        return document.RootElement
            .GetProperty("commits")[0]
            .GetProperty("commitId")
            .GetString()
            ?? throw new InvalidOperationException("Azure DevOps did not return the initial commit.");
    }

    private string RequireOwner()
    {
        var owner = _repository.Owner;
        if (!string.IsNullOrWhiteSpace(owner))
        {
            return owner;
        }

        owner = GetOptional("default.owner");
        if (!string.IsNullOrWhiteSpace(owner))
        {
            return owner;
        }

        throw new InvalidOperationException(
            "The provider did not return a repository owner, which branch seeding needs.");
    }

    private string DefaultBranch =>
        string.IsNullOrWhiteSpace(_repository.DefaultBranch) ? "main" : _repository.DefaultBranch;

    private string GetRequired(string key)
    {
        var value = GetOptional(key);
        if (value is null)
        {
            throw new InvalidOperationException(
                $"The '{key}' setting is required for provider '{_configuration.ProviderName}'.");
        }

        return value;
    }

    private string? GetOptional(string key)
    {
        return _configuration.Settings.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    private static string FileNameFor(string branchName)
    {
        return $"vcsmanager-{branchName.Replace('/', '-')}.txt";
    }

    private static string ContentFor(string branchName)
    {
        return $"VcsManager integration test for {branchName} at {DateTimeOffset.UtcNow:O}.";
    }

    private static string ToBase64(string branchName)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(ContentFor(branchName)));
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Length > 500)
        {
            body = body[..500];
        }

        throw new InvalidOperationException(
            $"{operation} failed with status {(int)response.StatusCode} " +
            $"({response.ReasonPhrase}). {body}");
    }
}
