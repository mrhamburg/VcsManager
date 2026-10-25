using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using VcsManager.Internal;

namespace VcsManager.AzureDevOps.Internal;

internal sealed class AzureDevOpsApiClient : IAzureDevOpsApiClient
{
    private const string ProductName = "VcsManager";

    private readonly GitHttpClient _gitClient;
    private readonly HttpClient _restClient;
    private readonly AzureDevOpsOptions _options;
    private readonly string _organization;
    private readonly string _organizationUrl;

    public AzureDevOpsApiClient(AzureDevOpsOptions options)
        : this(options, CreateGitClient(options), CreateRestClient(options))
    {
    }

    internal AzureDevOpsApiClient(
        AzureDevOpsOptions options,
        GitHttpClient gitClient,
        HttpClient restClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(gitClient);
        ArgumentNullException.ThrowIfNull(restClient);
        options.Validate();

        _options = options;
        _gitClient = gitClient;
        _restClient = restClient;
        _organization = options.GetOrganizationName();
        _organizationUrl = options.OrganizationUrl
            ?? throw new InvalidOperationException(
                "AzureDevOpsOptions.OrganizationUrl must be set before the client is created.");
    }

    internal Uri BaseAddress => _restClient.BaseAddress ?? _options.CreateOrganizationUri();

    internal AzureDevOpsOptions Options => _options;

    public async Task<AzureDevOpsRepository> CreateRepositoryAsync(
        string project,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _gitClient.CreateRepositoryAsync(
            new GitRepositoryCreateOptions { Name = request.Name },
            project,
            sourceRef: null,
            userState: null,
            cancellationToken: cancellationToken);

        return MapRepository(created, project);
    }

    public async Task SeedRepositoryAsync(
        string project,
        string name,
        SeedRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var repositoryPath =
            $"{Uri.EscapeDataString(project.Trim())}/_apis/git/repositories/" +
            $"{Uri.EscapeDataString(name.Trim())}";

        var refs = await SendRestAsync<AzureDevOpsRefListResponse>(
            ProviderHttp.AddQuery(
                $"{repositoryPath}/refs",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["api-version"] = _options.ApiVersion,
                    ["filter"] = "heads/",
                }),
            cancellationToken);

        if (refs.Value is { Count: > 0 })
        {
            throw new VcsConflictException(
                $"Azure DevOps repository '{project}/{name}' already has a commit.");
        }

        var push = new AzureDevOpsCreatePushRequest
        {
            RefUpdates =
            [
                new AzureDevOpsRefUpdate
                {
                    Name = $"refs/heads/{request.Branch}",
                    OldObjectId = new string('0', 40),
                },
            ],
            Commits =
            [
                new AzureDevOpsCommit
                {
                    Comment = request.CommitMessage ?? "Initial commit",
                    Changes = request.Files
                        .Select(file => new AzureDevOpsChange
                        {
                            ChangeType = "add",
                            Item = new AzureDevOpsItem { Path = $"/{file.Path}" },
                            NewContent = new AzureDevOpsItemContent
                            {
                                Content = Convert.ToBase64String(file.Content.Span),
                                ContentType = "base64Encoded",
                            },
                        })
                        .ToArray(),
                },
            ],
        };

        await SendRestContentAsync(
            HttpMethod.Post,
            ProviderHttp.AddQuery(
                $"{repositoryPath}/pushes",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["api-version"] = _options.ApiVersion,
                }),
            push,
            cancellationToken);
    }

    public async Task<AzureDevOpsRepository> GetRepositoryAsync(
        string project,
        string name,
        CancellationToken cancellationToken)
    {
        var repository = await _gitClient.GetRepositoryAsync(
            project,
            name,
            includeParent: null,
            userState: null,
            cancellationToken: cancellationToken);

        return MapRepository(repository, project);
    }

    public async Task DeleteRepositoryAsync(
        string project,
        string name,
        CancellationToken cancellationToken)
    {
        var repository = await _gitClient.GetRepositoryAsync(
            project,
            name,
            includeParent: null,
            userState: null,
            cancellationToken: cancellationToken);

        await _gitClient.DeleteRepositoryAsync(
            project,
            repository.Id,
            userState: null,
            cancellationToken: cancellationToken);
    }

    public async Task<AzureDevOpsPage<AzureDevOpsRepository>> ListRepositoriesAsync(
        string? project,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        // The REST API 7.1 repository listing returns all repositories and does
        // not support $top or $skip, so the page is cut on the client.
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["api-version"] = _options.ApiVersion,
        };

        var path = string.IsNullOrWhiteSpace(project)
            ? "_apis/git/repositories"
            : $"{Uri.EscapeDataString(project.Trim())}/_apis/git/repositories";

        var response = await SendRestAsync<AzureDevOpsRepositoryListResponse>(
            ProviderHttp.AddQuery(path, parameters),
            cancellationToken);

        var items = response.Value
            .Skip(GetSkip(pagination))
            .Take(pagination.PageSize + 1)
            .Select(repository => MapRepository(repository, project))
            .ToArray();

        var hasNextPage = items.Length > pagination.PageSize;
        if (hasNextPage)
        {
            items = items[..pagination.PageSize];
        }

        return new AzureDevOpsPage<AzureDevOpsRepository>(items, hasNextPage, TotalCount: null);
    }

    public async Task<AzureDevOpsPullRequest> CreatePullRequestAsync(
        string project,
        string repositoryName,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _gitClient.CreatePullRequestAsync(
            new GitPullRequest
            {
                SourceRefName = AzureDevOpsRefs.ToRef(request.SourceBranch),
                TargetRefName = AzureDevOpsRefs.ToRef(request.TargetBranch),
                Title = request.Title,
                Description = request.Description,
                IsDraft = request.Draft,
            },
            project,
            repositoryName,
            supportsIterations: null,
            userState: null,
            cancellationToken: cancellationToken);

        return MapPullRequest(created, project, repositoryName);
    }

    public async Task<AzureDevOpsPullRequest> GetPullRequestAsync(
        string project,
        string repositoryName,
        int number,
        CancellationToken cancellationToken)
    {
        var pullRequest = await _gitClient.GetPullRequestAsync(
            project,
            repositoryName,
            number,
            userState: null,
            cancellationToken: cancellationToken);

        return MapPullRequest(pullRequest, project, repositoryName);
    }

    public async Task<AzureDevOpsPage<AzureDevOpsPullRequest>> ListPullRequestsAsync(
        string project,
        string repositoryName,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        var criteria = new GitPullRequestSearchCriteria
        {
            Status = MapStatus(query.State),
            SourceRefName = string.IsNullOrWhiteSpace(query.SourceBranch)
                ? null
                : AzureDevOpsRefs.ToRef(query.SourceBranch),
            TargetRefName = string.IsNullOrWhiteSpace(query.TargetBranch)
                ? null
                : AzureDevOpsRefs.ToRef(query.TargetBranch),
        };

        var pullRequests = await _gitClient.GetPullRequestsAsync(
            project,
            repositoryName,
            criteria,
            maxCommentLength: null,
            skip: GetSkip(pagination),
            top: pagination.PageSize + 1,
            userState: null,
            cancellationToken: cancellationToken);

        var items = pullRequests
            .Select(pullRequest => MapPullRequest(pullRequest, project, repositoryName))
            .ToArray();

        var hasNextPage = items.Length > pagination.PageSize;
        if (hasNextPage)
        {
            items = items[..pagination.PageSize];
        }

        return new AzureDevOpsPage<AzureDevOpsPullRequest>(items, hasNextPage, TotalCount: null);
    }

    public async Task<AzureDevOpsPullRequest> CompletePullRequestAsync(
        string project,
        string repositoryName,
        int number,
        string sourceCommitId,
        MergePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var update = new GitPullRequest
        {
            Status = PullRequestStatus.Completed,
            LastMergeSourceCommit = new GitCommitRef { CommitId = sourceCommitId },
            CompletionOptions = new GitPullRequestCompletionOptions
            {
                DeleteSourceBranch = request.DeleteSourceBranch,
                MergeStrategy = AzureDevOpsMapper.ToMergeStrategy(request.Method),
                SquashMerge = request.Method == MergeMethod.Squash,
                MergeCommitMessage = ProviderHttp.BuildCommitMessage(request.CommitTitle, request.CommitMessage),
            },
        };

        var completed = await _gitClient.UpdatePullRequestAsync(
            update,
            project,
            repositoryName,
            number,
            userState: null,
            cancellationToken: cancellationToken);

        return MapPullRequest(completed, project, repositoryName);
    }

    public async Task<AzureDevOpsPullRequest> AbandonPullRequestAsync(
        string project,
        string repositoryName,
        int number,
        CancellationToken cancellationToken)
    {
        var update = new GitPullRequest { Status = PullRequestStatus.Abandoned };

        var abandoned = await _gitClient.UpdatePullRequestAsync(
            update,
            project,
            repositoryName,
            number,
            userState: null,
            cancellationToken: cancellationToken);

        return MapPullRequest(abandoned, project, repositoryName);
    }

    public ValueTask DisposeAsync()
    {
        _gitClient.Dispose();
        _restClient.Dispose();
        return ValueTask.CompletedTask;
    }

    private static GitHttpClient CreateGitClient(AzureDevOpsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        return new GitHttpClient(
            options.CreateOrganizationUri(),
            new VssBasicCredential(string.Empty, options.PersonalAccessToken));
    }

    private static HttpClient CreateRestClient(AzureDevOpsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var client = new HttpClient { BaseAddress = options.CreateOrganizationUri() };
        var credential = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($":{options.PersonalAccessToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credential);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(ProductName);

        return client;
    }

    private Task<T> SendRestAsync<T>(string path, CancellationToken cancellationToken)
    {
        return SendRestAsync<T>(HttpMethod.Get, path, cancellationToken);
    }

    private async Task<T> SendRestAsync<T>(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        using var response = await _restClient.SendAsync(request, cancellationToken);

        await ThrowIfFailedAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync(GetTypeInfo<T>(), cancellationToken);

        return result ?? throw new VcsProviderException(
            "Azure DevOps returned an empty response body.");
    }

    private async Task SendRestContentAsync<TRequest>(
        HttpMethod method,
        string path,
        TRequest body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body, GetTypeInfo<TRequest>()),
        };
        using var response = await _restClient.SendAsync(request, cancellationToken);

        await ThrowIfFailedAsync(response, cancellationToken);
    }

    private static async Task ThrowIfFailedAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        // An invalid or expired PAT often returns 203 with an HTML sign-in page.
        if (response.IsSuccessStatusCode &&
            (response.StatusCode == HttpStatusCode.NonAuthoritativeInformation ||
                response.Content.Headers.ContentType?.MediaType == "text/html"))
        {
            throw new AzureDevOpsApiException(HttpStatusCode.Unauthorized, string.Empty);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AzureDevOpsApiException(
                response.StatusCode,
                body,
                ProviderHttp.GetRetryAfter(response));
        }
    }

    private static int GetSkip(PaginationOptions pagination)
    {
        var skip = (long)(pagination.Page - 1) * pagination.PageSize;
        return (int)Math.Min(skip, int.MaxValue);
    }

    private AzureDevOpsRepository MapRepository(GitRepository repository, string project)
    {
        return new AzureDevOpsRepository
        {
            Id = repository.Id,
            Name = repository.Name,
            Project = repository.ProjectReference?.Name ?? project,
            Organization = _organization,
            WebUrl = string.IsNullOrWhiteSpace(repository.WebUrl)
                ? BuildRepositoryWebUrl(repository.ProjectReference?.Name ?? project, repository.Name)
                : repository.WebUrl,
            DefaultBranch = repository.DefaultBranch,
        };
    }

    private AzureDevOpsRepository MapRepository(
        AzureDevOpsRepositoryResponse repository,
        string? fallbackProject)
    {
        var project = repository.Project?.Name ?? fallbackProject ?? string.Empty;
        var name = repository.Name ?? string.Empty;

        return new AzureDevOpsRepository
        {
            Id = repository.Id,
            Name = name,
            Project = project,
            Organization = _organization,
            WebUrl = string.IsNullOrWhiteSpace(repository.WebUrl)
                ? BuildRepositoryWebUrl(project, name)
                : repository.WebUrl,
            DefaultBranch = repository.DefaultBranch,
        };
    }

    private AzureDevOpsPullRequest MapPullRequest(
        GitPullRequest pullRequest,
        string project,
        string repositoryName)
    {
        return AzureDevOpsMapper.FromSdkPullRequest(
            pullRequest,
            _organizationUrl,
            project,
            repositoryName);
    }

    private string BuildRepositoryWebUrl(string project, string name)
    {
        return $"{_organizationUrl.TrimEnd('/')}/{Uri.EscapeDataString(project)}/_git/{Uri.EscapeDataString(name)}";
    }

    private static JsonTypeInfo<T> GetTypeInfo<T>()
    {
        return AzureDevOpsJsonContext.Default.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
            ?? throw new InvalidOperationException(
                $"No JSON serialization metadata is registered for {typeof(T)}.");
    }

    private static PullRequestStatus MapStatus(PullRequestState? state)
    {
        return state switch
        {
            PullRequestState.Open or PullRequestState.Draft => PullRequestStatus.Active,
            PullRequestState.Closed => PullRequestStatus.Abandoned,
            PullRequestState.Merged => PullRequestStatus.Completed,
            _ => PullRequestStatus.All,
        };
    }
}
