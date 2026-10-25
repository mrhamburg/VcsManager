using System.Globalization;
using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

internal sealed class FakeGiteaApiClient : IGiteaApiClient
{
    private readonly Dictionary<string, GiteaRepository> _repositories =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, GiteaPullRequest> _pullRequests =
        new(StringComparer.Ordinal);
    private long _nextRepositoryId = 1;
    private long _nextPullRequestId = 1;
    private int _nextPullRequestNumber = 1;

    public bool MergeConflict { get; set; }

    public MergeMethod? UnsupportedMergeMethod { get; set; }

    public int MergeCallCount { get; private set; }

    public int CloseCallCount { get; private set; }

    public Task<GiteaRepository> CreateRepositoryAsync(
        string owner,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var key = RepositoryKey(owner, request.Name);

        if (_repositories.ContainsKey(key))
        {
            throw new VcsConflictException($"Repository '{owner}/{request.Name}' already exists.");
        }

        var repository = new GiteaRepository
        {
            Id = _nextRepositoryId++,
            Name = request.Name.Trim(),
            Owner = owner,
            FullName = $"{owner}/{request.Name}",
            WebUrl = WebUrl(owner, request.Name),
            DefaultBranch = request.DefaultBranch ?? (request.InitializeWithReadme ? "main" : null),
            Visibility = request.Visibility,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };

        _repositories[key] = repository;
        return Task.FromResult(repository);
    }

    public Task<GiteaRepository> GetRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken)
    {
        if (!_repositories.TryGetValue(RepositoryKey(owner, name), out var repository))
        {
            throw new RepositoryNotFoundException(owner, project: null, name);
        }

        return Task.FromResult(repository);
    }

    public Task DeleteRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken)
    {
        if (!_repositories.Remove(RepositoryKey(owner, name)))
        {
            throw new RepositoryNotFoundException(owner, project: null, name);
        }

        return Task.CompletedTask;
    }

    public Task<GiteaPage<GiteaRepository>> ListRepositoriesAsync(
        string? owner,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        IEnumerable<GiteaRepository> items = _repositories.Values;

        if (!string.IsNullOrWhiteSpace(owner))
        {
            items = items.Where(repository =>
                string.Equals(repository.Owner, owner, StringComparison.Ordinal));
        }

        return Task.FromResult(
            Paginate(
                items.OrderBy(repository => repository.FullName, StringComparer.Ordinal),
                pagination));
    }

    public Task<GiteaPullRequest> CreatePullRequestAsync(
        string owner,
        string repositoryName,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        EnsureRepository(owner, repositoryName);

        var number = _nextPullRequestNumber++;
        var title = GiteaDraftTitle.Apply(request.Title, request.Draft);
        var isDraft = request.Draft || GiteaDraftTitle.IsDraft(title);

        var pullRequest = new GiteaPullRequest
        {
            Id = _nextPullRequestId++,
            Number = number,
            Title = title,
            Description = request.Description,
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            State = isDraft ? PullRequestState.Draft : PullRequestState.Open,
            IsDraft = isDraft,
            Author = "conformance-user",
            WebUrl = $"{WebUrl(owner, repositoryName)}/pulls/{number}",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };

        _pullRequests[PullRequestKey(owner, repositoryName, number)] = pullRequest;
        return Task.FromResult(pullRequest);
    }

    public Task<GiteaPullRequest> GetPullRequestAsync(
        string owner,
        string repositoryName,
        int number,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(GetPullRequest(owner, repositoryName, number));
    }

    public Task<GiteaPage<GiteaPullRequest>> ListPullRequestsAsync(
        string owner,
        string repositoryName,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        EnsureRepository(owner, repositoryName);

        var prefix = $"{owner}|{repositoryName}|";
        IEnumerable<GiteaPullRequest> items = _pullRequests
            .Where(entry => entry.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(entry => entry.Value);

        if (query.State is { } state)
        {
            items = items.Where(pullRequest => pullRequest.State == state);
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            items = items.Where(pullRequest =>
                string.Equals(pullRequest.Author, query.Author, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.SourceBranch))
        {
            items = items.Where(pullRequest =>
                string.Equals(pullRequest.SourceBranch, query.SourceBranch, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(query.TargetBranch))
        {
            items = items.Where(pullRequest =>
                string.Equals(pullRequest.TargetBranch, query.TargetBranch, StringComparison.Ordinal));
        }

        return Task.FromResult(Paginate(
            items.OrderBy(pullRequest => pullRequest.Number),
            pagination));
    }

    public Task<GiteaPullRequest> MergePullRequestAsync(
        string owner,
        string repositoryName,
        int number,
        MergePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        MergeCallCount++;

        if (UnsupportedMergeMethod == request.Method)
        {
            throw new VcsUnsupportedOperationException(
                "gitea",
                $"MergePullRequest with MergeMethod.{request.Method}");
        }

        if (MergeConflict)
        {
            throw new VcsConflictException("The pull request cannot be merged.");
        }

        var current = GetPullRequest(owner, repositoryName, number);

        if (current.State == PullRequestState.Merged)
        {
            return Task.FromResult(current);
        }

        if (current.State == PullRequestState.Closed)
        {
            throw new VcsConflictException("The pull request is closed.");
        }

        var merged = current with
        {
            State = PullRequestState.Merged,
            IsDraft = false,
            MergedAt = DateTimeOffset.UnixEpoch,
        };

        _pullRequests[PullRequestKey(owner, repositoryName, number)] = merged;
        return Task.FromResult(merged);
    }

    public Task<GiteaPullRequest> ClosePullRequestAsync(
        string owner,
        string repositoryName,
        int number,
        CancellationToken cancellationToken)
    {
        CloseCallCount++;

        var current = GetPullRequest(owner, repositoryName, number);

        if (current.State is PullRequestState.Closed or PullRequestState.Merged)
        {
            return Task.FromResult(current);
        }

        var closed = current with
        {
            State = PullRequestState.Closed,
            IsDraft = false,
            ClosedAt = DateTimeOffset.UnixEpoch,
        };

        _pullRequests[PullRequestKey(owner, repositoryName, number)] = closed;
        return Task.FromResult(closed);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private GiteaPullRequest GetPullRequest(string owner, string repositoryName, int number)
    {
        EnsureRepository(owner, repositoryName);

        if (!_pullRequests.TryGetValue(PullRequestKey(owner, repositoryName, number), out var pullRequest))
        {
            throw new PullRequestNotFoundException(
                new RepositoryRef { Owner = owner, Name = repositoryName },
                number);
        }

        return pullRequest;
    }

    private void EnsureRepository(string owner, string repositoryName)
    {
        if (!_repositories.ContainsKey(RepositoryKey(owner, repositoryName)))
        {
            throw new RepositoryNotFoundException(owner, project: null, repositoryName);
        }
    }

    private static GiteaPage<T> Paginate<T>(IEnumerable<T> source, PaginationOptions pagination)
    {
        var all = source.ToArray();
        var pageSize = GiteaPagination.EffectivePageSize(pagination.PageSize);
        var skip = (pagination.Page - 1) * pageSize;
        var items = all
            .Skip(skip)
            .Take(pageSize)
            .ToArray();

        return new GiteaPage<T>(
            items,
            skip + items.Length < all.Length,
            all.Length);
    }

    private static string WebUrl(string owner, string name)
    {
        return $"https://gitea.example.com/{owner}/{name}";
    }

    private static string RepositoryKey(string owner, string name)
    {
        return $"{owner}|{name}";
    }

    private static string PullRequestKey(string owner, string repositoryName, int number)
    {
        return $"{owner}|{repositoryName}|{number.ToString(CultureInfo.InvariantCulture)}";
    }
}
