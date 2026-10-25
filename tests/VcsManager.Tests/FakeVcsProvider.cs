namespace VcsManager.Tests;

internal sealed class FakeVcsProvider : IVcsProvider
{
    private const string KeySeparator = "\u001F";
    private readonly Dictionary<string, VcsRepository> _repositories = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PullRequest> _pullRequests = new(StringComparer.Ordinal);
    private readonly bool _mergeConflict;
    private readonly MergeMethod? _unsupportedMergeMethod;
    private int _nextPullRequestNumber = 1;

    public FakeVcsProvider(bool mergeConflict = false, MergeMethod? unsupportedMergeMethod = null)
    {
        _mergeConflict = mergeConflict;
        _unsupportedMergeMethod = unsupportedMergeMethod;
    }

    public bool IsDisposed { get; private set; }

    public Task<VcsRepository> CreateRepositoryAsync(
        CreateRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fullName = RepositoryFullName(request.Owner, request.Project, request.Name);
        var repository = new VcsRepository
        {
            Id = fullName,
            Name = request.Name,
            Owner = request.Owner,
            Project = request.Project,
            FullName = fullName,
            Url = $"https://example.test/{fullName}",
            DefaultBranch = request.DefaultBranch,
            Visibility = request.Visibility,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };

        _repositories[RepositoryKey(request.Owner, request.Project, request.Name)] = repository;
        return Task.FromResult(repository);
    }

    public Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (!_repositories.TryGetValue(RepositoryKey(repository), out var found))
        {
            throw new RepositoryNotFoundException(repository.Owner, repository.Project, repository.Name);
        }

        return Task.FromResult(found);
    }

    public Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        query ??= new RepositoryQuery();
        pagination ??= new PaginationOptions();

        IEnumerable<VcsRepository> items = _repositories.Values;
        if (!string.IsNullOrWhiteSpace(query.Owner))
        {
            items = items.Where(repository => repository.Owner == query.Owner);
        }

        if (!string.IsNullOrWhiteSpace(query.Project))
        {
            items = items.Where(repository => repository.Project == query.Project);
        }

        if (query.Visibility is { } visibility)
        {
            items = items.Where(repository => repository.Visibility == visibility);
        }

        if (!string.IsNullOrWhiteSpace(query.NameContains))
        {
            items = items.Where(
                repository => repository.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase));
        }

        if (!query.IncludeArchived)
        {
            items = items.Where(repository => !repository.IsArchived);
        }

        var ordered = items.OrderBy(repository => repository.Name, StringComparer.Ordinal).ToArray();
        var pageItems = ordered
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToArray();

        return Task.FromResult(new PagedResult<VcsRepository>
        {
            Items = pageItems,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = ordered.Length,
            HasNextPage = pagination.Page * pagination.PageSize < ordered.Length,
        });
    }

    public Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (!_repositories.Remove(RepositoryKey(repository)))
        {
            throw new RepositoryNotFoundException(repository.Owner, repository.Project, repository.Name);
        }

        return Task.CompletedTask;
    }

    public Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return Task.FromResult(_repositories.ContainsKey(RepositoryKey(repository)));
    }

    public Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(request);
        EnsureRepositoryExists(repository);

        var number = _nextPullRequestNumber++;
        var pullRequest = new PullRequest
        {
            Id = $"{RepositoryKey(repository)}#{number}",
            Number = number,
            Title = request.Title,
            Description = request.Description,
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            State = request.Draft ? PullRequestState.Draft : PullRequestState.Open,
            IsDraft = request.Draft,
            Author = "fake-user",
            Url = $"https://example.test/{RepositoryFullName(repository)}/pull/{number}",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };

        _pullRequests[PullRequestKey(repository, number)] = pullRequest;
        return Task.FromResult(pullRequest);
    }

    public Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        EnsureRepositoryExists(repository);
        return Task.FromResult(GetPullRequestOrThrow(repository, number));
    }

    public Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        EnsureRepositoryExists(repository);

        query ??= new PullRequestQuery();
        pagination ??= new PaginationOptions();

        var repositoryKey = RepositoryKey(repository) + "#";
        IEnumerable<PullRequest> items = _pullRequests
            .Where(entry => entry.Key.StartsWith(repositoryKey, StringComparison.Ordinal))
            .Select(entry => entry.Value);

        if (query.State is { } state)
        {
            items = items.Where(pullRequest => pullRequest.State == state);
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            items = items.Where(
                pullRequest => pullRequest.Author.Equals(query.Author, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.SourceBranch))
        {
            items = items.Where(pullRequest => pullRequest.SourceBranch == query.SourceBranch);
        }

        if (!string.IsNullOrWhiteSpace(query.TargetBranch))
        {
            items = items.Where(pullRequest => pullRequest.TargetBranch == query.TargetBranch);
        }

        var ordered = items.OrderBy(pullRequest => pullRequest.Number).ToArray();
        var pageItems = ordered
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToArray();

        return Task.FromResult(new PagedResult<PullRequest>
        {
            Items = pageItems,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = ordered.Length,
            HasNextPage = pagination.Page * pagination.PageSize < ordered.Length,
        });
    }

    public Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        EnsureRepositoryExists(repository);

        var method = request?.Method ?? MergeMethod.Merge;
        if (_unsupportedMergeMethod == method)
        {
            throw new VcsUnsupportedOperationException("fake", "MergePullRequest");
        }

        if (_mergeConflict)
        {
            throw new VcsConflictException("The pull request has merge conflicts.");
        }

        var existing = GetPullRequestOrThrow(repository, number);
        if (existing.State == PullRequestState.Merged)
        {
            return Task.FromResult(existing);
        }

        if (existing.State == PullRequestState.Closed)
        {
            throw new VcsConflictException("A closed pull request cannot be merged.");
        }

        var merged = existing with
        {
            State = PullRequestState.Merged,
            IsDraft = false,
            MergedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };

        _pullRequests[PullRequestKey(repository, number)] = merged;
        return Task.FromResult(merged);
    }

    public Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        EnsureRepositoryExists(repository);

        var existing = GetPullRequestOrThrow(repository, number);
        if (existing.State is PullRequestState.Closed or PullRequestState.Merged)
        {
            return Task.FromResult(existing);
        }

        var closed = existing with
        {
            State = PullRequestState.Closed,
            IsDraft = false,
            ClosedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };

        _pullRequests[PullRequestKey(repository, number)] = closed;
        return Task.FromResult(closed);
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }

    private void EnsureRepositoryExists(RepositoryRef repository)
    {
        if (!_repositories.ContainsKey(RepositoryKey(repository)))
        {
            throw new RepositoryNotFoundException(repository.Owner, repository.Project, repository.Name);
        }
    }

    private PullRequest GetPullRequestOrThrow(RepositoryRef repository, int number)
    {
        if (!_pullRequests.TryGetValue(PullRequestKey(repository, number), out var pullRequest))
        {
            throw new PullRequestNotFoundException(repository, number);
        }

        return pullRequest;
    }

    private static string PullRequestKey(RepositoryRef repository, int number)
    {
        return $"{RepositoryKey(repository)}#{number}";
    }

    private static string RepositoryKey(RepositoryRef repository)
    {
        return RepositoryKey(repository.Owner, repository.Project, repository.Name);
    }

    private static string RepositoryKey(string? owner, string? project, string name)
    {
        return string.Join(KeySeparator, owner ?? string.Empty, project ?? string.Empty, name);
    }

    private static string RepositoryFullName(RepositoryRef repository)
    {
        return RepositoryFullName(repository.Owner, repository.Project, repository.Name);
    }

    private static string RepositoryFullName(string? owner, string? project, string name)
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(owner))
        {
            parts.Add(owner);
        }

        if (!string.IsNullOrWhiteSpace(project))
        {
            parts.Add(project);
        }

        parts.Add(name);
        return string.Join("/", parts);
    }
}
