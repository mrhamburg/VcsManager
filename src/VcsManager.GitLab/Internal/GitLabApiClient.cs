using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using VcsManager.Internal;

namespace VcsManager.GitLab.Internal;

internal sealed class GitLabApiClient : IGitLabApiClient
{
    private const string ProductName = "VcsManager";

    private readonly HttpClient _client;
    private readonly bool _disposeClient;

    public GitLabApiClient(GitLabOptions options)
        : this(options, new HttpClientHandler(), disposeHandler: true)
    {
    }

    internal GitLabApiClient(
        GitLabOptions options,
        HttpMessageHandler handler,
        bool disposeHandler = true)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(handler);
        options.Validate();

        _client = CreateClient(options, handler, disposeHandler);
        _disposeClient = true;
        BaseAddress = _client.BaseAddress ?? options.CreateApiBaseAddress();
    }

    internal GitLabApiClient(GitLabOptions options, HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(client);
        options.Validate();

        _client = client;
        _disposeClient = false;
        ConfigureClient(_client, options);
        BaseAddress = _client.BaseAddress ?? options.CreateApiBaseAddress();
    }

    internal Uri BaseAddress { get; }

    public async Task<GitLabProject> CreateProjectAsync(
        string? @namespace,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var namespaceId = await ResolveNamespaceIdAsync(@namespace, cancellationToken);
        var body = new GitLabCreateProjectRequest
        {
            Name = request.Name,
            Path = request.Name,
            Description = request.Description,
            Visibility = MapVisibility(request.Visibility),
            InitializeWithReadme = request.InitializeWithReadme,
            DefaultBranch = request.DefaultBranch,
            NamespaceId = namespaceId,
        };

        var response = await SendAsync<GitLabCreateProjectRequest, GitLabProjectResponse>(
            HttpMethod.Post,
            "projects",
            body,
            cancellationToken);

        return MapProject(response);
    }

    public async Task<GitLabProject> GetProjectAsync(
        string projectPath,
        CancellationToken cancellationToken)
    {
        var response = await SendAsync<GitLabProjectResponse>(
            HttpMethod.Get,
            $"projects/{GitLabProjectPath.Encode(projectPath)}",
            cancellationToken);

        return MapProject(response);
    }

    public async Task DeleteProjectAsync(
        string projectPath,
        CancellationToken cancellationToken)
    {
        await SendDeleteAsync(
            $"projects/{GitLabProjectPath.Encode(projectPath)}",
            cancellationToken);
    }

    public async Task<GitLabPage<GitLabProject>> ListProjectsAsync(
        string? @namespace,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        var parameters = CreatePaginationParameters(pagination);
        string path;

        if (!string.IsNullOrWhiteSpace(@namespace))
        {
            path = $"groups/{Uri.EscapeDataString(@namespace.Trim())}/projects";
            parameters["include_subgroups"] = "true";
        }
        else
        {
            path = "projects";
        }

        if (!query.IncludeArchived)
        {
            parameters["archived"] = "false";
        }

        if (query.Visibility is { } visibility)
        {
            parameters["visibility"] = MapVisibility(visibility);
        }

        if (!string.IsNullOrWhiteSpace(query.NameContains))
        {
            parameters["search"] = query.NameContains.Trim();
        }

        var page = await SendPageAsync<GitLabProjectResponse>(
            ProviderHttp.AddQuery(path, parameters),
            cancellationToken);

        return new GitLabPage<GitLabProject>(
            page.Items.Select(MapProject).ToArray(),
            page.HasNextPage,
            page.TotalCount);
    }

    public async Task<GitLabMergeRequest> CreateMergeRequestAsync(
        string projectPath,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var body = new GitLabCreateMergeRequestRequest
        {
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            Title = GitLabDraftTitle.Apply(request.Title, request.Draft),
            Description = request.Description,
        };

        var response = await SendAsync<GitLabCreateMergeRequestRequest, GitLabMergeRequestResponse>(
            HttpMethod.Post,
            $"projects/{GitLabProjectPath.Encode(projectPath)}/merge_requests",
            body,
            cancellationToken);

        return MapMergeRequest(response);
    }

    public async Task<GitLabMergeRequest> GetMergeRequestAsync(
        string projectPath,
        int iid,
        CancellationToken cancellationToken)
    {
        var response = await SendAsync<GitLabMergeRequestResponse>(
            HttpMethod.Get,
            $"projects/{GitLabProjectPath.Encode(projectPath)}/merge_requests/{iid}",
            cancellationToken);

        return MapMergeRequest(response);
    }

    public async Task<GitLabPage<GitLabMergeRequest>> ListMergeRequestsAsync(
        string projectPath,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        var parameters = CreatePaginationParameters(pagination);
        parameters["state"] = MapStateParameter(query.State);

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            parameters["author_username"] = query.Author.Trim();
        }

        if (!string.IsNullOrWhiteSpace(query.SourceBranch))
        {
            parameters["source_branch"] = query.SourceBranch.Trim();
        }

        if (!string.IsNullOrWhiteSpace(query.TargetBranch))
        {
            parameters["target_branch"] = query.TargetBranch.Trim();
        }

        var path = ProviderHttp.AddQuery(
            $"projects/{GitLabProjectPath.Encode(projectPath)}/merge_requests",
            parameters);

        var page = await SendPageAsync<GitLabMergeRequestResponse>(path, cancellationToken);

        return new GitLabPage<GitLabMergeRequest>(
            page.Items.Select(MapMergeRequest).ToArray(),
            page.HasNextPage,
            page.TotalCount);
    }

    public async Task<GitLabMergeRequest> MergeMergeRequestAsync(
        string projectPath,
        int iid,
        MergePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var commitMessage = ProviderHttp.BuildCommitMessage(request.CommitTitle, request.CommitMessage);
        var body = new GitLabMergeRequestMergeRequest
        {
            MergeCommitMessage = commitMessage,
            SquashCommitMessage = request.Method == MergeMethod.Squash ? commitMessage : null,
            ShouldRemoveSourceBranch = request.DeleteSourceBranch ? true : null,
            Squash = request.Method == MergeMethod.Squash ? true : null,
        };

        var response = await SendAsync<GitLabMergeRequestMergeRequest, GitLabMergeRequestResponse>(
            HttpMethod.Put,
            $"projects/{GitLabProjectPath.Encode(projectPath)}/merge_requests/{iid}/merge",
            body,
            cancellationToken);

        return MapMergeRequest(response);
    }

    public async Task<GitLabMergeRequest> CloseMergeRequestAsync(
        string projectPath,
        int iid,
        CancellationToken cancellationToken)
    {
        var body = new GitLabUpdateMergeRequestRequest { StateEvent = "close" };

        var response = await SendAsync<GitLabUpdateMergeRequestRequest, GitLabMergeRequestResponse>(
            HttpMethod.Put,
            $"projects/{GitLabProjectPath.Encode(projectPath)}/merge_requests/{iid}",
            body,
            cancellationToken);

        return MapMergeRequest(response);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposeClient)
        {
            _client.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private static HttpClient CreateClient(
        GitLabOptions options,
        HttpMessageHandler handler,
        bool disposeHandler)
    {
        var client = new HttpClient(handler, disposeHandler);
        ConfigureClient(client, options);
        return client;
    }

    private static void ConfigureClient(HttpClient client, GitLabOptions options)
    {
        client.BaseAddress = options.CreateApiBaseAddress();
        client.DefaultRequestHeaders.Remove("PRIVATE-TOKEN");
        client.DefaultRequestHeaders.TryAddWithoutValidation("PRIVATE-TOKEN", options.Token);
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(ProductName);
    }

    private async Task<long?> ResolveNamespaceIdAsync(
        string? @namespace,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(@namespace))
        {
            return null;
        }

        var value = @namespace.Trim();
        if (long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            return id;
        }

        var response = await SendAsync<GitLabNamespaceResponse>(
            HttpMethod.Get,
            $"namespaces/{Uri.EscapeDataString(value)}",
            cancellationToken);

        if (response.Id <= 0)
        {
            throw new VcsProviderException(
                $"GitLab did not return a namespace id for '{value}'.");
        }

        return response.Id;
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        using var response = await _client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        HttpMethod method,
        string path,
        TRequest body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body, GetTypeInfo<TRequest>()),
        };
        using var response = await _client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadAsync<TResponse>(response, cancellationToken);
    }

    private async Task<GitLabPage<T>> SendPageAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await _client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var items = await ReadAsync<List<T>>(response, cancellationToken);
        return new GitLabPage<T>(
            items,
            HasNextPage(response),
            TotalCount(response));
    }

    private async Task SendDeleteAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        using var response = await _client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var result = await response.Content.ReadFromJsonAsync(GetTypeInfo<T>(), cancellationToken);

        return result ?? throw new VcsProviderException(
            "GitLab returned an empty response body.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new GitLabApiException(response.StatusCode, body, ProviderHttp.GetRetryAfter(response));
    }

    private static bool HasNextPage(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("X-Next-Page", out var values) &&
            values.Any(value => !string.IsNullOrWhiteSpace(value));
    }

    private static int? TotalCount(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("X-Total", out var values))
        {
            return null;
        }

        var value = values.FirstOrDefault();
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var total)
            ? total
            : null;
    }

    private static Dictionary<string, string> CreatePaginationParameters(
        PaginationOptions pagination)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["page"] = pagination.Page.ToString(CultureInfo.InvariantCulture),
            ["per_page"] = pagination.PageSize.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static JsonTypeInfo<T> GetTypeInfo<T>()
    {
        return GitLabJsonContext.Default.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
            ?? throw new InvalidOperationException(
                $"No JSON serialization metadata is registered for {typeof(T)}.");
    }

    private static string MapVisibility(VcsVisibility visibility)
    {
        return visibility switch
        {
            VcsVisibility.Internal => "internal",
            VcsVisibility.Public => "public",
            _ => "private",
        };
    }

    private static VcsVisibility MapVisibility(string? visibility)
    {
        return visibility?.ToLowerInvariant() switch
        {
            "internal" => VcsVisibility.Internal,
            "public" => VcsVisibility.Public,
            _ => VcsVisibility.Private,
        };
    }

    private static string MapStateParameter(PullRequestState? state)
    {
        return state switch
        {
            PullRequestState.Open or PullRequestState.Draft => "opened",
            PullRequestState.Closed => "closed",
            PullRequestState.Merged => "merged",
            _ => "all",
        };
    }

    private static GitLabProject MapProject(GitLabProjectResponse response)
    {
        var pathWithNamespace = response.PathWithNamespace;
        var segments = pathWithNamespace.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var owner = segments.Length > 1
            ? segments[0]
            : response.Namespace?.Path;
        var project = segments.Length > 2
            ? string.Join("/", segments[1..^1])
            : null;

        return new GitLabProject
        {
            Id = response.Id,
            Name = response.Path,
            Path = response.Path,
            PathWithNamespace = pathWithNamespace,
            WebUrl = response.WebUrl,
            DefaultBranch = response.DefaultBranch,
            Visibility = MapVisibility(response.Visibility),
            Description = response.Description,
            IsArchived = response.Archived,
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt,
            Owner = owner,
            Project = project,
        };
    }

    private static GitLabMergeRequest MapMergeRequest(GitLabMergeRequestResponse response)
    {
        var isDraft = response.Draft ||
            response.WorkInProgress ||
            GitLabDraftTitle.IsDraft(response.Title);

        return new GitLabMergeRequest
        {
            Id = response.Id,
            Iid = response.Iid,
            Title = response.Title,
            Description = response.Description,
            SourceBranch = response.SourceBranch,
            TargetBranch = response.TargetBranch,
            State = MapPullRequestState(response, isDraft),
            IsDraft = isDraft,
            Author = response.Author?.Username ?? string.Empty,
            WebUrl = response.WebUrl,
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt,
            MergedAt = response.MergedAt,
            ClosedAt = response.ClosedAt,
        };
    }

    private static PullRequestState MapPullRequestState(
        GitLabMergeRequestResponse response,
        bool isDraft)
    {
        if (response.MergedAt is not null ||
            string.Equals(response.State, "merged", StringComparison.OrdinalIgnoreCase))
        {
            return PullRequestState.Merged;
        }

        if (string.Equals(response.State, "closed", StringComparison.OrdinalIgnoreCase))
        {
            return PullRequestState.Closed;
        }

        return isDraft ? PullRequestState.Draft : PullRequestState.Open;
    }
}
