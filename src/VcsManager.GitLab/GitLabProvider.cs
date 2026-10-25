using VcsManager.GitLab.Internal;

namespace VcsManager.GitLab;

/// <summary>
/// GitLab implementation of <see cref="IVcsProvider"/>.
/// </summary>
public sealed class GitLabProvider : IVcsProvider
{
    private readonly IGitLabApiClient _client;
    private readonly GitLabOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitLabProvider"/> class.
    /// </summary>
    /// <param name="options">The GitLab options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The options are invalid.</exception>
    public GitLabProvider(GitLabOptions options)
        : this(CreateClient(options), options)
    {
    }

    internal GitLabProvider(IGitLabApiClient client, GitLabOptions options)
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
        var repository = CreateRepositoryRef(owner, request.Project, request.Name);
        var namespacePath = GitLabProjectPath.CombineNamespace(owner, request.Project);

        try
        {
            var project = await _client.CreateProjectAsync(
                namespacePath,
                request,
                cancellationToken);
            return GitLabMapper.ToRepository(project);
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
            throw GitLabExceptionMapper.MapRepository(
                exception,
                "CreateRepository",
                repository);
        }
    }

    /// <inheritdoc/>
    public async Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveRepository(repository);
        var path = GitLabProjectPath.From(resolved, _options.DefaultNamespace);

        try
        {
            return GitLabMapper.ToRepository(
                await _client.GetProjectAsync(path, cancellationToken));
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
            throw GitLabExceptionMapper.MapRepository(
                exception,
                "GetRepository",
                resolved);
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

        var owner = ResolveOptionalOwner(query.Owner);
        var namespacePath = owner is null
            ? null
            : GitLabProjectPath.CombineNamespace(owner, query.Project);

        try
        {
            var page = await _client.ListProjectsAsync(
                namespacePath,
                query,
                pagination,
                cancellationToken);

            return new PagedResult<VcsRepository>
            {
                Items = page.Items
                    .Where(project => MatchesRepositoryQuery(project, query))
                    .Select(GitLabMapper.ToRepository)
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
            throw GitLabExceptionMapper.MapOperation(exception, "ListRepositories");
        }
    }

    /// <inheritdoc/>
    public async Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveRepository(repository);
        var path = GitLabProjectPath.From(resolved, _options.DefaultNamespace);

        try
        {
            await _client.DeleteProjectAsync(path, cancellationToken);
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
            throw GitLabExceptionMapper.MapRepository(
                exception,
                "DeleteRepository",
                resolved);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveRepository(repository);
        var path = GitLabProjectPath.From(resolved, _options.DefaultNamespace);

        try
        {
            await _client.GetProjectAsync(path, cancellationToken);
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
            var mapped = GitLabExceptionMapper.MapRepository(
                exception,
                "RepositoryExists",
                resolved);

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
        var resolved = ResolveRepository(repository);
        var path = GitLabProjectPath.From(resolved, _options.DefaultNamespace);
        ValidateCreatePullRequestRequest(request);

        try
        {
            var mergeRequest = await _client.CreateMergeRequestAsync(
                path,
                request,
                cancellationToken);
            return GitLabMapper.ToPullRequest(mergeRequest);
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
            throw GitLabExceptionMapper.MapRepository(
                exception,
                "CreatePullRequest",
                resolved);
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveRepository(repository);
        var path = GitLabProjectPath.From(resolved, _options.DefaultNamespace);
        ValidatePullRequestNumber(number);

        try
        {
            var mergeRequest = await _client.GetMergeRequestAsync(
                path,
                number,
                cancellationToken);
            return GitLabMapper.ToPullRequest(mergeRequest);
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
            throw GitLabExceptionMapper.MapPullRequest(
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
        var resolved = ResolveRepository(repository);
        var path = GitLabProjectPath.From(resolved, _options.DefaultNamespace);
        query = NormalizePullRequestQuery(query ?? new PullRequestQuery());
        pagination ??= new PaginationOptions();

        try
        {
            var page = await _client.ListMergeRequestsAsync(
                path,
                query,
                pagination,
                cancellationToken);

            return new PagedResult<PullRequest>
            {
                Items = page.Items
                    .Where(mergeRequest => MatchesPullRequestQuery(mergeRequest, query))
                    .Select(GitLabMapper.ToPullRequest)
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
            throw GitLabExceptionMapper.MapRepository(
                exception,
                "ListPullRequests",
                resolved);
        }
    }

    /// <inheritdoc/>
    public async Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveRepository(repository);
        var path = GitLabProjectPath.From(resolved, _options.DefaultNamespace);
        ValidatePullRequestNumber(number);
        request ??= new MergePullRequestRequest();
        ValidateMergeMethod(request.Method);

        try
        {
            var mergeRequest = await _client.MergeMergeRequestAsync(
                path,
                number,
                request,
                cancellationToken);
            return GitLabMapper.ToPullRequest(mergeRequest);
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
            throw GitLabExceptionMapper.MapMergePullRequest(
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
        var resolved = ResolveRepository(repository);
        var path = GitLabProjectPath.From(resolved, _options.DefaultNamespace);
        ValidatePullRequestNumber(number);

        GitLabMergeRequest current;
        try
        {
            current = await _client.GetMergeRequestAsync(path, number, cancellationToken);
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
            throw GitLabExceptionMapper.MapPullRequest(
                exception,
                "ClosePullRequest",
                resolved,
                number);
        }

        if (current.State is PullRequestState.Closed or PullRequestState.Merged)
        {
            return GitLabMapper.ToPullRequest(current);
        }

        try
        {
            var closed = await _client.CloseMergeRequestAsync(path, number, cancellationToken);
            return GitLabMapper.ToPullRequest(closed);
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
            throw GitLabExceptionMapper.MapPullRequest(
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
        var resolved = string.IsNullOrWhiteSpace(owner) ? _options.DefaultNamespace : owner;

        return RequireValue(
            resolved,
            "A GitLab namespace is required. Set Owner or GitLabOptions.DefaultNamespace.",
            nameof(owner));
    }

    private string? ResolveOptionalOwner(string? owner)
    {
        var resolved = string.IsNullOrWhiteSpace(owner) ? _options.DefaultNamespace : owner;
        return string.IsNullOrWhiteSpace(resolved) ? null : resolved.Trim();
    }

    private RepositoryRef ResolveRepository(RepositoryRef repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var owner = ResolveOwner(repository.Owner);
        var name = RequireValue(
            repository.Name,
            "A repository name is required.",
            nameof(repository));

        return repository with
        {
            Owner = owner,
            Project = NormalizeOptional(repository.Project),
            Name = name,
        };
    }

    private static RepositoryRef CreateRepositoryRef(string owner, string? project, string name)
    {
        return new RepositoryRef
        {
            Owner = owner,
            Project = NormalizeOptional(project),
            Name = name.Trim(),
        };
    }

    private static void ValidateCreateRepositoryRequest(CreateRepositoryRequest request)
    {
        RequireValue(request.Name, "A repository name is required.", nameof(request));
    }

    private static void ValidateMergeMethod(MergeMethod method)
    {
        if (method == MergeMethod.Rebase)
        {
            throw new VcsUnsupportedOperationException(
                "gitlab",
                "MergePullRequest with MergeMethod.Rebase");
        }

        if (method is not (MergeMethod.Merge or MergeMethod.Squash))
        {
            throw new ArgumentOutOfRangeException(
                nameof(method),
                method,
                "The merge method is not valid.");
        }
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

    private static bool MatchesPullRequestQuery(GitLabMergeRequest mergeRequest, PullRequestQuery query)
    {
        if (query.State is { } state && mergeRequest.State != state)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Author) &&
            !string.Equals(mergeRequest.Author, query.Author, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.SourceBranch) &&
            !string.Equals(mergeRequest.SourceBranch, query.SourceBranch, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.TargetBranch) &&
            !string.Equals(mergeRequest.TargetBranch, query.TargetBranch, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static bool MatchesRepositoryQuery(GitLabProject project, RepositoryQuery query)
    {
        if (!query.IncludeArchived && project.IsArchived)
        {
            return false;
        }

        if (query.Visibility is { } visibility && project.Visibility != visibility)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(query.NameContains) ||
            project.Path.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase) ||
            project.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string RequireValue(string? value, string message, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }

    private static IGitLabApiClient CreateClient(GitLabOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return new GitLabApiClient(options);
    }
}
