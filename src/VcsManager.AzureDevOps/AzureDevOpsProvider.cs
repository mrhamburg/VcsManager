using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps;

/// <summary>
/// Azure DevOps implementation of <see cref="IVcsProvider"/>.
/// </summary>
public sealed class AzureDevOpsProvider : IVcsProvider
{
    private const string ProviderName = "azuredevops";

    private readonly IAzureDevOpsApiClient _client;
    private readonly AzureDevOpsOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureDevOpsProvider"/> class.
    /// </summary>
    /// <param name="options">The Azure DevOps options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The options are invalid.</exception>
    public AzureDevOpsProvider(AzureDevOpsOptions options)
        : this(CreateClient(options), options)
    {
    }

    internal AzureDevOpsProvider(IAzureDevOpsApiClient client, AzureDevOpsOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        _client = client;
        _options = options;
    }

    /// <inheritdoc/>
    public async Task<VcsRepository> CreateRepositoryAsync(
        CreateRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCreateRepositoryRequest(request);

        var owner = ResolveOwner(request.Owner);
        var project = ResolveProject(request.Project);
        var name = request.Name.Trim();

        try
        {
            var created = await _client.CreateRepositoryAsync(project, request, cancellationToken);
            return AzureDevOpsMapper.ToRepository(created);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw AzureDevOpsExceptionMapper.MapRepository(
                exception,
                "CreateRepository",
                owner,
                project,
                name);
        }
    }

    /// <inheritdoc/>
    public async Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        var (owner, project, name) = ResolveRepository(repository);

        try
        {
            return AzureDevOpsMapper.ToRepository(
                await _client.GetRepositoryAsync(project, name, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw AzureDevOpsExceptionMapper.MapRepository(
                exception,
                "GetRepository",
                owner,
                project,
                name);
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        query = NormalizeRepositoryQuery(query ?? new RepositoryQuery());
        pagination ??= new PaginationOptions();

        var owner = ResolveOwner(query.Owner);
        var project = string.IsNullOrWhiteSpace(query.Project)
            ? _options.DefaultProject
            : query.Project;
        project = string.IsNullOrWhiteSpace(project) ? null : project.Trim();

        try
        {
            var page = await _client.ListRepositoriesAsync(
                project,
                query,
                pagination,
                cancellationToken);

            return new PagedResult<VcsRepository>
            {
                Items = page.Items
                    .Where(repository => MatchesRepositoryQuery(repository, query))
                    .Select(AzureDevOpsMapper.ToRepository)
                    .ToArray(),
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = page.TotalCount,
                HasNextPage = page.HasNextPage,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw AzureDevOpsExceptionMapper.MapOperation(exception, "ListRepositories");
        }
    }

    /// <inheritdoc/>
    public async Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        var (owner, project, name) = ResolveRepository(repository);

        try
        {
            await _client.DeleteRepositoryAsync(project, name, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw AzureDevOpsExceptionMapper.MapRepository(
                exception,
                "DeleteRepository",
                owner,
                project,
                name);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        var (owner, project, name) = ResolveRepository(repository);

        try
        {
            await _client.GetRepositoryAsync(project, name, cancellationToken);
            return true;
        }
        catch (RepositoryNotFoundException)
        {
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var mapped = AzureDevOpsExceptionMapper.MapRepository(
                exception,
                "RepositoryExists",
                owner,
                project,
                name);

            if (mapped is RepositoryNotFoundException)
            {
                return false;
            }

            throw mapped;
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (owner, project, name) = ResolveRepository(repository);
        ValidateCreatePullRequestRequest(request);

        try
        {
            var created = await _client.CreatePullRequestAsync(
                project,
                name,
                request,
                cancellationToken);
            return AzureDevOpsMapper.ToPullRequest(created);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw AzureDevOpsExceptionMapper.MapRepository(
                exception,
                "CreatePullRequest",
                owner,
                project,
                name);
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        var (owner, project, name) = ResolveRepository(repository);
        ValidatePullRequestNumber(number);
        var resolved = RepositoryRefOf(owner, project, name);

        try
        {
            var pullRequest = await _client.GetPullRequestAsync(
                project,
                name,
                number,
                cancellationToken);
            return AzureDevOpsMapper.ToPullRequest(pullRequest);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw AzureDevOpsExceptionMapper.MapPullRequest(
                exception,
                "GetPullRequest",
                resolved,
                number);
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        var (owner, project, name) = ResolveRepository(repository);
        query = NormalizePullRequestQuery(query ?? new PullRequestQuery());
        pagination ??= new PaginationOptions();

        try
        {
            var page = await _client.ListPullRequestsAsync(
                project,
                name,
                query,
                pagination,
                cancellationToken);

            return new PagedResult<PullRequest>
            {
                Items = page.Items
                    .Where(pullRequest => MatchesPullRequestQuery(pullRequest, query))
                    .Select(AzureDevOpsMapper.ToPullRequest)
                    .ToArray(),
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = page.TotalCount,
                HasNextPage = page.HasNextPage,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw AzureDevOpsExceptionMapper.MapRepository(
                exception,
                "ListPullRequests",
                owner,
                project,
                name);
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var (owner, project, name) = ResolveRepository(repository);
        ValidatePullRequestNumber(number);
        request ??= new MergePullRequestRequest();
        ValidateMergeMethod(request.Method);
        var resolved = RepositoryRefOf(owner, project, name);

        AzureDevOpsPullRequest current;
        try
        {
            current = await _client.GetPullRequestAsync(project, name, number, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw AzureDevOpsExceptionMapper.MapPullRequest(
                exception,
                "MergePullRequest",
                resolved,
                number);
        }

        if (current.State is PullRequestState.Merged or PullRequestState.Closed)
        {
            throw new VcsConflictException(
                $"Pull request #{number} in repository '{resolved.Name}' is not active.");
        }

        if (string.IsNullOrWhiteSpace(current.SourceCommitId))
        {
            throw new VcsConflictException(
                $"Pull request #{number} in repository '{resolved.Name}' has no source commit to complete.");
        }

        try
        {
            var merged = await _client.CompletePullRequestAsync(
                project,
                name,
                number,
                current.SourceCommitId,
                request,
                cancellationToken);

            return AzureDevOpsMapper.ToPullRequest(merged);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw AzureDevOpsExceptionMapper.MapMergePullRequest(
                exception,
                "MergePullRequest",
                resolved,
                number);
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        var (owner, project, name) = ResolveRepository(repository);
        ValidatePullRequestNumber(number);
        var resolved = RepositoryRefOf(owner, project, name);

        AzureDevOpsPullRequest current;
        try
        {
            current = await _client.GetPullRequestAsync(project, name, number, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw AzureDevOpsExceptionMapper.MapPullRequest(
                exception,
                "ClosePullRequest",
                resolved,
                number);
        }

        if (current.State is PullRequestState.Closed or PullRequestState.Merged)
        {
            return AzureDevOpsMapper.ToPullRequest(current);
        }

        try
        {
            var closed = await _client.AbandonPullRequestAsync(
                project,
                name,
                number,
                cancellationToken);

            return AzureDevOpsMapper.ToPullRequest(closed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw AzureDevOpsExceptionMapper.MapPullRequest(
                exception,
                "ClosePullRequest",
                resolved,
                number);
        }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }

    private string ResolveProject(string? project)
    {
        var resolved = string.IsNullOrWhiteSpace(project) ? _options.DefaultProject : project;

        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new ArgumentException(
                "An Azure DevOps team project is required. Set Project or AzureDevOpsOptions.DefaultProject.",
                nameof(project));
        }

        return resolved.Trim();
    }

    private string ResolveOwner(string? owner)
    {
        var organization = _options.GetOrganizationName();
        var resolved = string.IsNullOrWhiteSpace(owner) ? organization : owner.Trim();

        if (!string.Equals(resolved, organization, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"The repository owner must match the configured Azure DevOps organization '{organization}'.",
                nameof(owner));
        }

        return resolved;
    }

    private (string Owner, string Project, string Name) ResolveRepository(RepositoryRef repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var owner = ResolveOwner(repository.Owner);
        var project = ResolveProject(repository.Project);
        var name = RequireValue(repository.Name, "A repository name is required.", nameof(repository));

        return (owner, project, name);
    }

    private static void ValidateCreateRepositoryRequest(CreateRepositoryRequest request)
    {
        RequireValue(request.Name, "A repository name is required.", nameof(request));

        if (request.InitializeWithReadme)
        {
            throw new VcsUnsupportedOperationException(
                ProviderName,
                "CreateRepository with InitializeWithReadme");
        }
    }

    private static RepositoryQuery NormalizeRepositoryQuery(RepositoryQuery query)
    {
        return query with
        {
            Owner = NormalizeOptional(query.Owner),
            Project = NormalizeOptional(query.Project),
            NameContains = NormalizeOptional(query.NameContains),
        };
    }

    private static PullRequestQuery NormalizePullRequestQuery(PullRequestQuery query)
    {
        return query with
        {
            Author = NormalizeOptional(query.Author),
            SourceBranch = NormalizeOptional(query.SourceBranch),
            TargetBranch = NormalizeOptional(query.TargetBranch),
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool MatchesRepositoryQuery(
        AzureDevOpsRepository repository,
        RepositoryQuery query)
    {
        if (query.Visibility is { } visibility && visibility != VcsVisibility.Private)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(query.NameContains) ||
            repository.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesPullRequestQuery(
        AzureDevOpsPullRequest pullRequest,
        PullRequestQuery query)
    {
        if (query.State is { } state && pullRequest.State != state)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Author) &&
            !string.Equals(pullRequest.Author, query.Author, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.SourceBranch) &&
            !string.Equals(pullRequest.SourceBranch, query.SourceBranch, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.TargetBranch) &&
            !string.Equals(pullRequest.TargetBranch, query.TargetBranch, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static void ValidateCreatePullRequestRequest(CreatePullRequestRequest request)
    {
        RequireValue(request.Title, "A pull request title is required.", nameof(request));
        RequireValue(request.SourceBranch, "A pull request source branch is required.", nameof(request));
        RequireValue(request.TargetBranch, "A pull request target branch is required.", nameof(request));
    }

    private static void ValidateMergeMethod(MergeMethod method)
    {
        if (!Enum.IsDefined(method))
        {
            throw new ArgumentOutOfRangeException(
                nameof(method),
                method,
                "The merge method is not valid.");
        }
    }

    private static void ValidatePullRequestNumber(int number)
    {
        if (number < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(number),
                number,
                "The pull request number must be at least 1.");
        }
    }

    private static string RequireValue(string? value, string message, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }

    private static IAzureDevOpsApiClient CreateClient(AzureDevOpsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return new AzureDevOpsApiClient(options);
    }

    private static RepositoryRef RepositoryRefOf(string owner, string project, string name)
    {
        return new RepositoryRef { Owner = owner, Project = project, Name = name };
    }
}
