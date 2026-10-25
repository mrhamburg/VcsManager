using VcsManager.Gitea.Internal;

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
    public Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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
