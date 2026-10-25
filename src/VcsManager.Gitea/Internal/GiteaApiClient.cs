using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using VcsManager.Internal;

namespace VcsManager.Gitea.Internal;

internal sealed class GiteaApiClient : IGiteaApiClient
{
    private const string ProductName = "VcsManager";

    private readonly HttpClient _client;
    private readonly bool _disposeClient;

    public GiteaApiClient(GiteaOptions options)
        : this(options, new HttpClientHandler(), disposeHandler: true)
    {
    }

    internal GiteaApiClient(
        GiteaOptions options,
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

    internal GiteaApiClient(GiteaOptions options, HttpClient client)
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

    public async Task<GiteaRepository> CreateRepositoryAsync(
        string owner,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var ownerInfo = await ResolveOwnerAsync(owner, cancellationToken);
        var body = new GiteaCreateRepositoryRequest
        {
            Name = request.Name,
            Description = request.Description,
            Private = request.Visibility == VcsVisibility.Private,
            AutoInit = request.InitializeWithReadme,
            DefaultBranch = request.DefaultBranch,
        };

        string path;
        if (ownerInfo.IsOrganization)
        {
            path = $"orgs/{Encode(owner)}/repos";
        }
        else
        {
            var currentUser = await SendAsync<GiteaUserResponse>(
                HttpMethod.Get,
                "user",
                cancellationToken);

            if (currentUser.Id != ownerInfo.Id)
            {
                throw new VcsUnsupportedOperationException(
                    "gitea",
                    "CreateRepository",
                    new InvalidOperationException(
                        $"Gitea cannot create a repository for user '{owner}' with the current token. " +
                        "The token user must own the repository."));
            }

            path = "user/repos";
        }

        var response = await SendAsync<GiteaCreateRepositoryRequest, GiteaRepositoryResponse>(
            HttpMethod.Post,
            path,
            body,
            cancellationToken);

        return GiteaMapper.FromRepositoryResponse(response);
    }

    public async Task SeedRepositoryAsync(
        string owner,
        string name,
        SeedRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var repository = await GetRepositoryAsync(owner, name, cancellationToken);
        if (!repository.IsEmpty)
        {
            throw new VcsConflictException(
                $"Gitea repository '{owner}/{name}' already has a commit.");
        }

        var body = new GiteaChangeFilesRequest
        {
            Message = request.CommitMessage ?? "Initial commit",
            Branch = request.Branch,
            Files = request.Files
                .Select(file => new GiteaChangeFileOperation
                {
                    Operation = "create",
                    Path = file.Path,
                    Content = Convert.ToBase64String(file.Content.Span),
                })
                .ToArray(),
        };

        await SendContentAsync(
            HttpMethod.Post,
            $"{RepositoryPath(owner, name)}/contents",
            body,
            cancellationToken);
    }

    public async Task<GiteaRepository> GetRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken)
    {
        var response = await SendAsync<GiteaRepositoryResponse>(
            HttpMethod.Get,
            RepositoryPath(owner, name),
            cancellationToken);

        return GiteaMapper.FromRepositoryResponse(response);
    }

    public async Task DeleteRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken)
    {
        await SendDeleteAsync(RepositoryPath(owner, name), cancellationToken);
    }

    public async Task<GiteaPage<GiteaRepository>> ListRepositoriesAsync(
        string? owner,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        var parameters = CreatePaginationParameters(pagination);
        string path;
        var search = true;

        if (string.IsNullOrWhiteSpace(owner))
        {
            path = "repos/search";
            ApplySearchFilters(parameters, query);
        }
        else
        {
            var ownerInfo = await ResolveOwnerAsync(owner.Trim(), cancellationToken);

            if (ownerInfo.IsOrganization)
            {
                path = $"orgs/{Encode(owner)}/repos";
                search = false;
            }
            else
            {
                path = "repos/search";
                parameters["uid"] = ownerInfo.Id.ToString(CultureInfo.InvariantCulture);
                ApplySearchFilters(parameters, query);
            }
        }

        if (search)
        {
            var page = await SendSearchPageAsync<GiteaRepositoryResponse>(
                ProviderHttp.AddQuery(path, parameters),
                cancellationToken);

            return new GiteaPage<GiteaRepository>(
                page.Items.Select(GiteaMapper.FromRepositoryResponse).ToArray(),
                page.HasNextPage,
                page.TotalCount);
        }

        var repositories = await SendPageAsync<GiteaRepositoryResponse>(
            ProviderHttp.AddQuery(path, parameters),
            cancellationToken);

        return new GiteaPage<GiteaRepository>(
            repositories.Items.Select(GiteaMapper.FromRepositoryResponse).ToArray(),
            repositories.HasNextPage,
            repositories.TotalCount);
    }

    public async Task<GiteaPullRequest> CreatePullRequestAsync(
        string owner,
        string repositoryName,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var body = new GiteaCreatePullRequestRequest
        {
            Head = request.SourceBranch,
            Base = request.TargetBranch,
            Title = GiteaDraftTitle.Apply(request.Title, request.Draft),
            Body = request.Description,
        };

        var response = await SendAsync<GiteaCreatePullRequestRequest, GiteaPullRequestResponse>(
            HttpMethod.Post,
            PullRequestPath(owner, repositoryName),
            body,
            cancellationToken);

        return GiteaMapper.FromPullRequestResponse(response);
    }

    public async Task<GiteaPullRequest> GetPullRequestAsync(
        string owner,
        string repositoryName,
        int number,
        CancellationToken cancellationToken)
    {
        var response = await SendAsync<GiteaPullRequestResponse>(
            HttpMethod.Get,
            PullRequestNumberPath(owner, repositoryName, number),
            cancellationToken);

        return GiteaMapper.FromPullRequestResponse(response);
    }

    public async Task<GiteaPage<GiteaPullRequest>> ListPullRequestsAsync(
        string owner,
        string repositoryName,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        var parameters = CreatePaginationParameters(pagination);
        parameters["state"] = MapStateParameter(query.State);

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            parameters["poster"] = query.Author.Trim();
        }

        if (!string.IsNullOrWhiteSpace(query.TargetBranch))
        {
            parameters["base_branch"] = query.TargetBranch.Trim();
        }

        var page = await SendPageAsync<GiteaPullRequestResponse>(
            ProviderHttp.AddQuery(PullRequestPath(owner, repositoryName), parameters),
            cancellationToken);

        return new GiteaPage<GiteaPullRequest>(
            page.Items.Select(GiteaMapper.FromPullRequestResponse).ToArray(),
            page.HasNextPage,
            page.TotalCount);
    }

    public async Task<GiteaPullRequest> MergePullRequestAsync(
        string owner,
        string repositoryName,
        int number,
        MergePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var body = new GiteaMergePullRequestRequest
        {
            Do = MapMergeStyle(request.Method),
            DeleteBranchAfterMerge = request.DeleteSourceBranch ? true : null,
            MergeTitleField = request.CommitTitle,
            MergeMessageField = request.CommitMessage,
        };

        await SendContentAsync(
            HttpMethod.Post,
            PullRequestNumberPath(owner, repositoryName, number) + "/merge",
            body,
            cancellationToken);

        return await GetPullRequestAsync(owner, repositoryName, number, cancellationToken);
    }

    public async Task<GiteaPullRequest> ClosePullRequestAsync(
        string owner,
        string repositoryName,
        int number,
        CancellationToken cancellationToken)
    {
        var body = new GiteaEditPullRequestRequest { State = "closed" };

        var response = await SendAsync<GiteaEditPullRequestRequest, GiteaPullRequestResponse>(
            HttpMethod.Patch,
            PullRequestNumberPath(owner, repositoryName, number),
            body,
            cancellationToken);

        return GiteaMapper.FromPullRequestResponse(response);
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
        GiteaOptions options,
        HttpMessageHandler handler,
        bool disposeHandler)
    {
        var client = new HttpClient(handler, disposeHandler);
        ConfigureClient(client, options);
        return client;
    }

    private static void ConfigureClient(HttpClient client, GiteaOptions options)
    {
        client.BaseAddress = options.CreateApiBaseAddress();
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "Authorization",
            $"token {options.Token}");
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(ProductName);
    }

    private async Task<GiteaOwner> ResolveOwnerAsync(
        string owner,
        CancellationToken cancellationToken)
    {
        try
        {
            var organization = await SendAsync<GiteaOrganizationResponse>(
                HttpMethod.Get,
                $"orgs/{Encode(owner)}",
                cancellationToken);

            return new GiteaOwner(organization.Id, IsOrganization: true);
        }
        catch (GiteaApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
        }

        var user = await SendAsync<GiteaUserResponse>(
            HttpMethod.Get,
            $"users/{Encode(owner)}",
            cancellationToken);

        return new GiteaOwner(user.Id, IsOrganization: false);
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

    private async Task SendContentAsync<TRequest>(
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
    }

    private async Task<GiteaPage<T>> SendPageAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await _client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var items = await ReadAsync<List<T>>(response, cancellationToken);
        return new GiteaPage<T>(
            items,
            HasNextPage(response),
            TotalCount(response));
    }

    private async Task<GiteaPage<T>> SendSearchPageAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await _client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var search = await ReadAsync<GiteaSearchResponse<T>>(response, cancellationToken);
        return new GiteaPage<T>(
            search.Data,
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
            "Gitea returned an empty response body.");
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
        throw new GiteaApiException(response.StatusCode, body, ProviderHttp.GetRetryAfter(response));
    }

    private static bool HasNextPage(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var values))
        {
            return false;
        }

        return values.Any(value =>
            value.Contains("rel=\"next\"", StringComparison.OrdinalIgnoreCase));
    }

    private static int? TotalCount(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("X-Total-Count", out var values))
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
            ["limit"] = GiteaPagination.EffectivePageSize(pagination.PageSize)
                .ToString(CultureInfo.InvariantCulture),
        };
    }

    private static void ApplySearchFilters(
        Dictionary<string, string> parameters,
        RepositoryQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.NameContains))
        {
            parameters["q"] = query.NameContains.Trim();
        }

        if (!query.IncludeArchived)
        {
            parameters["archived"] = "false";
        }

        if (query.Visibility is { } visibility)
        {
            parameters["is_private"] = visibility == VcsVisibility.Private ? "true" : "false";
        }
    }

    private static string RepositoryPath(string owner, string name)
    {
        return $"repos/{Encode(owner)}/{Encode(name)}";
    }

    private static string PullRequestPath(string owner, string name)
    {
        return $"{RepositoryPath(owner, name)}/pulls";
    }

    private static string PullRequestNumberPath(string owner, string name, int number)
    {
        return $"{PullRequestPath(owner, name)}/{number.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string Encode(string value)
    {
        return Uri.EscapeDataString(value.Trim());
    }

    private static JsonTypeInfo<T> GetTypeInfo<T>()
    {
        return GiteaJsonContext.Default.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
            ?? throw new InvalidOperationException(
                $"No JSON serialization metadata is registered for {typeof(T)}.");
    }

    private static string MapStateParameter(PullRequestState? state)
    {
        return state switch
        {
            PullRequestState.Open or PullRequestState.Draft => "open",
            PullRequestState.Closed or PullRequestState.Merged => "closed",
            _ => "all",
        };
    }

    private static string MapMergeStyle(MergeMethod method)
    {
        return method switch
        {
            MergeMethod.Squash => "squash",
            MergeMethod.Rebase => "rebase",
            _ => "merge",
        };
    }
}
