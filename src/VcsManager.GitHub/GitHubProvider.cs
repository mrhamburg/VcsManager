using System.Text.RegularExpressions;
using VcsManager.GitHub.Internal;
using VcsManager.Internal;

namespace VcsManager.GitHub;

/// <summary>
/// GitHub implementation of <see cref="IVcsProvider"/>.
/// </summary>
public sealed partial class GitHubProvider : IVcsProvider
{
    private readonly IGitHubApiClient _client;
    private readonly GitHubOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubProvider"/> class.
    /// </summary>
    /// <param name="options">The GitHub options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The options are invalid.</exception>
    public GitHubProvider(GitHubOptions options)
        : this(CreateClient(options), options)
    {
    }

    internal GitHubProvider(IGitHubApiClient client, GitHubOptions options)
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
        request = NormalizeCreateRepositoryRequest(request);

        var owner = NormalizeOwner(request.Owner) ?? NormalizeOwner(_options.DefaultOwner);
        if (owner is not null)
        {
            RequirePathSegment(owner, "The repository owner", nameof(request));
        }

        try
        {
            var repository = await _client.CreateRepositoryAsync(owner, request, cancellationToken);
            return GitHubMapper.ToRepository(repository);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception) when (
            request.Visibility == VcsVisibility.Internal &&
            GitHubExceptionMapper.IsInternalVisibilityUnsupported(exception))
        {
            throw new VcsUnsupportedOperationException(
                VcsProviderNames.GitHub,
                "CreateRepository",
                exception);
        }
        catch (Exception exception)
        {
            throw GitHubExceptionMapper.MapRepository(
                exception,
                "CreateRepository",
                owner,
                request.Name);
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

        await ProviderOperation.ExecuteAsync(
            token => _client.SeedRepositoryAsync(owner, name, normalized, token),
            exception => GitHubExceptionMapper.MapRepository(
                exception,
                "SeedRepository",
                owner,
                name),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        var (owner, name) = ResolveRepository(repository);

        return await ProviderOperation.ExecuteAsync(
            async token => GitHubMapper.ToRepository(
                await _client.GetRepositoryAsync(owner, name, token)),
            exception => GitHubExceptionMapper.MapRepository(
                exception,
                "GetRepository",
                owner,
                name),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        query = QueryNormalization.Normalize(query ?? new RepositoryQuery());
        pagination ??= new PaginationOptions();
        ValidateRepositoryQuery(query);

        var owner = NormalizeOwner(query.Owner) ?? NormalizeOwner(_options.DefaultOwner);

        return await ProviderOperation.ExecuteAsync(
            async token =>
            {
                var page = await _client.ListRepositoriesAsync(owner, query, pagination, token);
                return new PagedResult<VcsRepository>
                {
                    Items = page.Items
                        .Where(repository => MatchesRepositoryQuery(repository, query))
                        .Select(GitHubMapper.ToRepository)
                        .ToArray(),
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalCount = null,
                    HasNextPage = page.HasNextPage,
                };
            },
            exception => GitHubExceptionMapper.MapOperation(exception, "ListRepositories"),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        var (owner, name) = ResolveRepository(repository);

        await ProviderOperation.ExecuteAsync(
            token => _client.DeleteRepositoryAsync(owner, name, token),
            exception => GitHubExceptionMapper.MapRepository(
                exception,
                "DeleteRepository",
                owner,
                name),
            cancellationToken);
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
            var mapped = GitHubExceptionMapper.MapRepository(
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
        request = NormalizeCreatePullRequestRequest(request);

        return await ProviderOperation.ExecuteAsync(
            async token => GitHubMapper.ToPullRequest(
                await _client.CreatePullRequestAsync(owner, name, request, token)),
            exception => GitHubExceptionMapper.MapRepository(
                exception,
                "CreatePullRequest",
                owner,
                name),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        var (owner, name) = ResolveRepository(repository);
        RequestValidation.ValidatePullRequestNumber(number);

        return await ProviderOperation.ExecuteAsync(
            async token => GitHubMapper.ToPullRequest(
                await _client.GetPullRequestAsync(owner, name, number, token)),
            exception => GitHubExceptionMapper.MapPullRequest(
                exception,
                "GetPullRequest",
                RepositoryOf(owner, name),
                number),
            cancellationToken);
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

        return await ProviderOperation.ExecuteAsync(
            async token =>
            {
                var page = await _client.ListPullRequestsAsync(
                    owner,
                    name,
                    query,
                    pagination,
                    token);

                return new PagedResult<PullRequest>
                {
                    Items = page.Items
                        .Where(pullRequest => QueryMatcher.Matches(query, pullRequest.State, pullRequest.Author, pullRequest.SourceBranch, pullRequest.TargetBranch))
                        .Select(GitHubMapper.ToPullRequest)
                        .ToArray(),
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalCount = null,
                    HasNextPage = page.HasNextPage,
                };
            },
            exception => GitHubExceptionMapper.MapRepository(
                exception,
                "ListPullRequests",
                owner,
                name),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var (owner, name) = ResolveRepository(repository);
        RequestValidation.ValidatePullRequestNumber(number);
        request ??= new MergePullRequestRequest();
        ValidateMergeMethod(request.Method);

        GitHubPullRequest merged;
        try
        {
            merged = await _client.MergePullRequestAsync(owner, name, number, request, cancellationToken);
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
            throw GitHubExceptionMapper.MapPullRequest(
                exception,
                "MergePullRequest",
                RepositoryOf(owner, name),
                number);
        }

        if (request.DeleteSourceBranch && !merged.SourceBranchIsFromFork)
        {
            try
            {
                await _client.DeleteBranchAsync(owner, name, merged.SourceBranch, cancellationToken);
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
                throw GitHubExceptionMapper.MapBranch(
                    exception,
                    "DeleteSourceBranch",
                    owner,
                    name,
                    merged.SourceBranch);
            }
        }

        return GitHubMapper.ToPullRequest(merged);
    }

    /// <inheritdoc/>
    public async Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        var (owner, name) = ResolveRepository(repository);
        RequestValidation.ValidatePullRequestNumber(number);

        GitHubPullRequest current;
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
            throw GitHubExceptionMapper.MapPullRequest(
                exception,
                "ClosePullRequest",
                RepositoryOf(owner, name),
                number);
        }

        if (current.State is PullRequestState.Closed or PullRequestState.Merged)
        {
            return GitHubMapper.ToPullRequest(current);
        }

        try
        {
            var closed = await _client.ClosePullRequestAsync(owner, name, number, cancellationToken);
            return GitHubMapper.ToPullRequest(closed);
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
            throw GitHubExceptionMapper.MapPullRequest(
                exception,
                "ClosePullRequest",
                RepositoryOf(owner, name),
                number);
        }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }

    private static IGitHubApiClient CreateClient(GitHubOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return new OctokitGitHubApiClient(options);
    }

    private (string Owner, string Name) ResolveRepository(RepositoryRef repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (!string.IsNullOrWhiteSpace(repository.Project))
        {
            throw new ArgumentException(
                "GitHub does not support the Project field on RepositoryRef.",
                nameof(repository));
        }

        var name = RequirePathSegment(
            RequestValidation.RequireValue(repository.Name, "A repository name is required.", nameof(repository)),
            "The repository name",
            nameof(repository));
        var owner = string.IsNullOrWhiteSpace(repository.Owner)
            ? _options.DefaultOwner
            : repository.Owner;

        var ownerValue = RequestValidation.RequireValue(
            owner,
            "A repository owner is required for GitHub. Set RepositoryRef.Owner or GitHubOptions.DefaultOwner.",
            nameof(repository));

        return (RequirePathSegment(ownerValue, "The repository owner", nameof(repository)), name);
    }

    private static CreateRepositoryRequest NormalizeCreateRepositoryRequest(CreateRepositoryRequest request)
    {
        var name = RequirePathSegment(
            RequestValidation.RequireValue(request.Name, "A repository name is required.", nameof(request)),
            "The repository name",
            nameof(request));

        if (!string.IsNullOrWhiteSpace(request.Project))
        {
            throw new ArgumentException(
                "GitHub does not support the Project field on CreateRepositoryRequest.",
                nameof(request));
        }

        return request with { Name = name };
    }

    private static void ValidateRepositoryQuery(RepositoryQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Project))
        {
            throw new ArgumentException(
                "GitHub does not support the Project field on RepositoryQuery.",
                nameof(query));
        }
    }

    private static CreatePullRequestRequest NormalizeCreatePullRequestRequest(CreatePullRequestRequest request)
    {
        return request with
        {
            Title = RequestValidation.RequireValue(request.Title, "A pull request title is required.", nameof(request)),
            SourceBranch = RequestValidation.RequireValue(request.SourceBranch, "A pull request source branch is required.", nameof(request)),
            TargetBranch = RequestValidation.RequireValue(request.TargetBranch, "A pull request target branch is required.", nameof(request)),
        };
    }

    private static void ValidateMergeMethod(MergeMethod method)
    {
        if (method is not (MergeMethod.Merge or MergeMethod.Squash or MergeMethod.Rebase))
        {
            throw new ArgumentOutOfRangeException(nameof(method), method, "The merge method is not valid.");
        }
    }

    private static bool MatchesRepositoryQuery(GitHubRepository repository, RepositoryQuery query)
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
            repository.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOwner(string? owner)
    {
        return QueryNormalization.NormalizeOptional(owner);
    }

    private static string RequirePathSegment(string value, string description, string parameterName)
    {
        if (value is "." or ".." || !PathSegmentPattern().IsMatch(value))
        {
            throw new ArgumentException(
                $"{description} may contain only letters, digits, '.', '_' and '-', and cannot be '.' or '..'.",
                parameterName);
        }

        return value;
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex PathSegmentPattern();

    private static RepositoryRef RepositoryOf(string owner, string name)
    {
        return new RepositoryRef { Owner = owner, Name = name };
    }
}
