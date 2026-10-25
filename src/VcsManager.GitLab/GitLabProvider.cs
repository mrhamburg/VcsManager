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
        query ??= new RepositoryQuery();
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
    public Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab merge request operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab merge request operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab merge request operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab merge request operations are added by a later milestone issue.");
    }

    /// <inheritdoc/>
    public Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("The GitLab merge request operations are added by a later milestone issue.");
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
