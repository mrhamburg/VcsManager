namespace VcsManager.Samples;

/// <summary>
/// In-memory provider used by the sample until provider packages are available.
/// </summary>
internal sealed class DemoVcsProvider : IVcsProvider
{
    private readonly Dictionary<RepositoryRef, VcsRepository> _repositories = [];
    private readonly Dictionary<(RepositoryRef Repository, int Number), PullRequest> _pullRequests = [];
    private int _nextPullRequestNumber = 1;

    public Task<VcsRepository> CreateRepositoryAsync(
        CreateRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var repositoryRef = new RepositoryRef
        {
            Owner = request.Owner,
            Project = request.Project,
            Name = request.Name,
        };
        var fullName = Format(repositoryRef);
        var now = DateTimeOffset.UtcNow;
        var repository = new VcsRepository
        {
            Id = fullName,
            Name = request.Name,
            Owner = request.Owner,
            Project = request.Project,
            FullName = fullName,
            Url = $"https://demo.local/{fullName}",
            DefaultBranch = request.DefaultBranch ?? "main",
            Visibility = request.Visibility,
            Description = request.Description,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _repositories[repositoryRef] = repository;
        return Task.FromResult(repository);
    }

    public Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetRepositoryOrThrow(repository));
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
            items = items.Where(repository =>
                repository.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase));
        }

        if (!query.IncludeArchived)
        {
            items = items.Where(repository => !repository.IsArchived);
        }

        return Task.FromResult(Page(
            items.OrderBy(repository => repository.Name, StringComparer.Ordinal).ToArray(),
            pagination));
    }

    public Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        if (!_repositories.Remove(repository))
        {
            throw new RepositoryNotFoundException(repository.Owner, repository.Project, repository.Name);
        }

        return Task.CompletedTask;
    }

    public Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_repositories.ContainsKey(repository));
    }

    public Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        GetRepositoryOrThrow(repository);

        var number = _nextPullRequestNumber++;
        var now = DateTimeOffset.UtcNow;
        var pullRequest = new PullRequest
        {
            Id = $"{Format(repository)}#{number}",
            Number = number,
            Title = request.Title,
            Description = request.Description,
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            State = request.Draft ? PullRequestState.Draft : PullRequestState.Open,
            IsDraft = request.Draft,
            Author = "demo-user",
            Url = $"https://demo.local/{Format(repository)}/pull/{number}",
            CreatedAt = now,
            UpdatedAt = now,
        };

        _pullRequests[(repository, number)] = pullRequest;
        return Task.FromResult(pullRequest);
    }

    public Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetPullRequestOrThrow(repository, number));
    }

    public Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        query ??= new PullRequestQuery();
        pagination ??= new PaginationOptions();

        IEnumerable<PullRequest> items = _pullRequests
            .Where(entry => entry.Key.Repository == repository)
            .Select(entry => entry.Value);

        if (query.State is { } state)
        {
            items = items.Where(pullRequest => pullRequest.State == state);
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            items = items.Where(pullRequest =>
                pullRequest.Author.Equals(query.Author, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.SourceBranch))
        {
            items = items.Where(pullRequest => pullRequest.SourceBranch == query.SourceBranch);
        }

        if (!string.IsNullOrWhiteSpace(query.TargetBranch))
        {
            items = items.Where(pullRequest => pullRequest.TargetBranch == query.TargetBranch);
        }

        return Task.FromResult(Page(
            items.OrderBy(pullRequest => pullRequest.Number).ToArray(),
            pagination));
    }

    public Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var pullRequest = GetPullRequestOrThrow(repository, number);
        if (pullRequest.State == PullRequestState.Closed)
        {
            throw new VcsConflictException($"Pull request #{number} is closed and cannot be merged.");
        }

        if (pullRequest.State == PullRequestState.Merged)
        {
            return Task.FromResult(pullRequest);
        }

        var merged = pullRequest with
        {
            State = PullRequestState.Merged,
            IsDraft = false,
            MergedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _pullRequests[(repository, number)] = merged;
        return Task.FromResult(merged);
    }

    public Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default)
    {
        var pullRequest = GetPullRequestOrThrow(repository, number);
        if (pullRequest.State is PullRequestState.Closed or PullRequestState.Merged)
        {
            return Task.FromResult(pullRequest);
        }

        var closed = pullRequest with
        {
            State = PullRequestState.Closed,
            IsDraft = false,
            ClosedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _pullRequests[(repository, number)] = closed;
        return Task.FromResult(closed);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private VcsRepository GetRepositoryOrThrow(RepositoryRef repository)
    {
        if (!_repositories.TryGetValue(repository, out var found))
        {
            throw new RepositoryNotFoundException(repository.Owner, repository.Project, repository.Name);
        }

        return found;
    }

    private PullRequest GetPullRequestOrThrow(RepositoryRef repository, int number)
    {
        if (!_pullRequests.TryGetValue((repository, number), out var pullRequest))
        {
            throw new PullRequestNotFoundException(repository, number);
        }

        return pullRequest;
    }

    private static PagedResult<T> Page<T>(IReadOnlyList<T> items, PaginationOptions pagination)
    {
        return new PagedResult<T>
        {
            Items = items
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToArray(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = items.Count,
            HasNextPage = pagination.Page * pagination.PageSize < items.Count,
        };
    }

    private static string Format(RepositoryRef repository)
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(repository.Owner))
        {
            parts.Add(repository.Owner);
        }

        if (!string.IsNullOrWhiteSpace(repository.Project))
        {
            parts.Add(repository.Project);
        }

        parts.Add(repository.Name);
        return string.Join("/", parts);
    }
}
