using VcsManager.Gitea.Internal;
using VcsManager.Internal;

namespace VcsManager.Gitea;

/// <summary>
/// Gitea implementation of <see cref="IVcsProvider"/>.
/// </summary>
public sealed class GiteaProvider : IVcsProvider
{
    private const string ProviderName = "gitea";

    private readonly IGiteaApiClient _client;
    private readonly GiteaOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GiteaProvider"/> class.
    /// </summary>
    /// <param name="options">The Gitea options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The options are invalid.</exception>
    public GiteaProvider(GiteaOptions options)
        : this(CreateClient(options), options)
    {
    }

    internal GiteaProvider(IGiteaApiClient client, GiteaOptions options)
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
        var name = request.Name.Trim();

        try
        {
            var created = await _client.CreateRepositoryAsync(owner, request, cancellationToken);
            return GiteaMapper.ToRepository(created);
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
            throw GiteaExceptionMapper.MapRepository(
                exception,
                "CreateRepository",
                owner,
                name);
        }
    }

    /// <inheritdoc/>
    public async Task SeedRepositoryAsync(
        RepositoryRef repository,
        SeedRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = SeedRequestValidator.Normalize(request);
        var (owner, name) = ResolveRepository(repository);

        try
        {
            await _client.SeedRepositoryAsync(owner, name, normalized, cancellationToken);
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
            throw GiteaExceptionMapper.MapRepository(
                exception,
                "SeedRepository",
                owner,
                name);
        }
    }

    /// <inheritdoc/>
    public async Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        var (owner, name) = ResolveRepository(repository);

        try
        {
            return GiteaMapper.ToRepository(
                await _client.GetRepositoryAsync(owner, name, cancellationToken));
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
            throw GiteaExceptionMapper.MapRepository(
                exception,
                "GetRepository",
                owner,
                name);
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        query = QueryNormalization.Normalize(query ?? new RepositoryQuery());
        pagination ??= new PaginationOptions();

        RejectProject(query.Project, nameof(query));
        RejectInternalVisibility(query.Visibility);
        var owner = ResolveOptionalOwner(query.Owner);

        try
        {
            var page = await _client.ListRepositoriesAsync(
                owner,
                query,
                pagination,
                cancellationToken);

            return new PagedResult<VcsRepository>
            {
                Items = page.Items
                    .Where(repository => MatchesRepositoryQuery(repository, query))
                    .Select(GiteaMapper.ToRepository)
                    .ToArray(),
                Page = pagination.Page,
                PageSize = GiteaPagination.EffectivePageSize(pagination.PageSize),
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
            throw GiteaExceptionMapper.MapOperation(exception, "ListRepositories");
        }
    }

    /// <inheritdoc/>
    public async Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        var (owner, name) = ResolveRepository(repository);

        try
        {
            await _client.DeleteRepositoryAsync(owner, name, cancellationToken);
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
            throw GiteaExceptionMapper.MapRepository(
                exception,
                "DeleteRepository",
                owner,
                name);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        var (owner, name) = ResolveRepository(repository);

        try
        {
            await _client.GetRepositoryAsync(owner, name, cancellationToken);
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
            var mapped = GiteaExceptionMapper.MapRepository(
                exception,
                "RepositoryExists",
                owner,
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
        var (owner, name) = ResolveRepository(repository);
        ValidateCreatePullRequestRequest(request);

        try
        {
            var created = await _client.CreatePullRequestAsync(
                owner,
                name,
                request,
                cancellationToken);
            return GiteaMapper.ToPullRequest(created);
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
            throw GiteaExceptionMapper.MapRepository(
                exception,
                "CreatePullRequest",
                owner,
                name);
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        var (owner, name) = ResolveRepository(repository);
        ValidatePullRequestNumber(number);
        var resolved = RepositoryRefOf(owner, name);

        try
        {
            var pullRequest = await _client.GetPullRequestAsync(
                owner,
                name,
                number,
                cancellationToken);
            return GiteaMapper.ToPullRequest(pullRequest);
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
            throw GiteaExceptionMapper.MapPullRequest(
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
        var (owner, name) = ResolveRepository(repository);
        query = QueryNormalization.Normalize(query ?? new PullRequestQuery());
        pagination ??= new PaginationOptions();

        try
        {
            var page = await _client.ListPullRequestsAsync(
                owner,
                name,
                query,
                pagination,
                cancellationToken);

            return new PagedResult<PullRequest>
            {
                Items = page.Items
                    .Where(pullRequest => QueryMatcher.Matches(query, pullRequest.State, pullRequest.Author, pullRequest.SourceBranch, pullRequest.TargetBranch))
                    .Select(GiteaMapper.ToPullRequest)
                    .ToArray(),
                Page = pagination.Page,
                PageSize = GiteaPagination.EffectivePageSize(pagination.PageSize),
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
            throw GiteaExceptionMapper.MapRepository(
                exception,
                "ListPullRequests",
                owner,
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
        var (owner, name) = ResolveRepository(repository);
        ValidatePullRequestNumber(number);
        request ??= new MergePullRequestRequest();
        ValidateMergeMethod(request.Method);
        var resolved = RepositoryRefOf(owner, name);

        GiteaPullRequest current;
        try
        {
            current = await _client.GetPullRequestAsync(owner, name, number, cancellationToken);
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
            throw GiteaExceptionMapper.MapPullRequest(
                exception,
                "MergePullRequest",
                resolved,
                number);
        }

        if (current.State == PullRequestState.Merged)
        {
            return GiteaMapper.ToPullRequest(current);
        }

        if (current.State == PullRequestState.Closed)
        {
            throw new VcsConflictException(
                $"Pull request #{number} in repository '{resolved.Name}' is closed and cannot be merged.");
        }

        try
        {
            var merged = await _client.MergePullRequestAsync(
                owner,
                name,
                number,
                request,
                cancellationToken);
            return GiteaMapper.ToPullRequest(merged);
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
            throw GiteaExceptionMapper.MapPullRequest(
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
        var (owner, name) = ResolveRepository(repository);
        ValidatePullRequestNumber(number);
        var resolved = RepositoryRefOf(owner, name);

        GiteaPullRequest current;
        try
        {
            current = await _client.GetPullRequestAsync(owner, name, number, cancellationToken);
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
            throw GiteaExceptionMapper.MapPullRequest(
                exception,
                "ClosePullRequest",
                resolved,
                number);
        }

        if (current.State is PullRequestState.Closed or PullRequestState.Merged)
        {
            return GiteaMapper.ToPullRequest(current);
        }

        try
        {
            var closed = await _client.ClosePullRequestAsync(
                owner,
                name,
                number,
                cancellationToken);
            return GiteaMapper.ToPullRequest(closed);
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
            throw GiteaExceptionMapper.MapPullRequest(
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

    private string ResolveOwner(string? owner)
    {
        var resolved = string.IsNullOrWhiteSpace(owner) ? _options.DefaultOwner : owner;

        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new ArgumentException(
                "A Gitea owner is required. Set Owner or GiteaOptions.DefaultOwner.",
                nameof(owner));
        }

        return resolved.Trim();
    }

    private string? ResolveOptionalOwner(string? owner)
    {
        var resolved = string.IsNullOrWhiteSpace(owner) ? _options.DefaultOwner : owner;
        return string.IsNullOrWhiteSpace(resolved) ? null : resolved.Trim();
    }

    private (string Owner, string Name) ResolveRepository(RepositoryRef repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        RejectProject(repository.Project, nameof(repository));

        var owner = ResolveOwner(repository.Owner);
        var name = RequireValue(repository.Name, "A repository name is required.", nameof(repository));

        return (owner, name);
    }

    private void ValidateCreateRepositoryRequest(CreateRepositoryRequest request)
    {
        RequireValue(request.Name, "A repository name is required.", nameof(request));
        RejectProject(request.Project, nameof(request));
        _ = ResolveOwner(request.Owner);

        if (request.Visibility == VcsVisibility.Internal)
        {
            throw new ArgumentException(
                "Gitea repositories support only Public and Private visibility.",
                nameof(request));
        }
    }

    private static void RejectProject(string? project, string parameterName)
    {
        if (!string.IsNullOrWhiteSpace(project))
        {
            throw new ArgumentException(
                "Gitea does not support project-scoped repositories. Remove Project from the request.",
                parameterName);
        }
    }

    private static void RejectInternalVisibility(VcsVisibility? visibility)
    {
        if (visibility == VcsVisibility.Internal)
        {
            throw new ArgumentException(
                "Gitea repositories support only Public and Private visibility.",
                nameof(visibility));
        }
    }

    private static bool MatchesRepositoryQuery(
        GiteaRepository repository,
        RepositoryQuery query)
    {
        if (!query.IncludeArchived && repository.IsArchived)
        {
            return false;
        }

        if (query.Visibility is { } visibility && repository.Visibility != visibility)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(query.NameContains) ||
            repository.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase) ||
            repository.FullName.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateCreatePullRequestRequest(CreatePullRequestRequest request)
    {
        RequireValue(request.Title, "A pull request title is required.", nameof(request));
        RequireValue(request.SourceBranch, "A pull request source branch is required.", nameof(request));
        RequireValue(request.TargetBranch, "A pull request target branch is required.", nameof(request));
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

    private static string RequireValue(string? value, string message, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }

    private static RepositoryRef RepositoryRefOf(string owner, string name)
    {
        return new RepositoryRef { Owner = owner, Name = name };
    }

    private static IGiteaApiClient CreateClient(GiteaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return new GiteaApiClient(options);
    }
}
