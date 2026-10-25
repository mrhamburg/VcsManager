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
        query ??= new RepositoryQuery();
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
    public Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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
