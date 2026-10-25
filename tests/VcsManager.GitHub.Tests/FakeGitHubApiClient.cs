using VcsManager.GitHub.Internal;

namespace VcsManager.GitHub.Tests;

internal sealed class FakeGitHubApiClient : IGitHubApiClient
{
    private readonly Dictionary<string, GitHubRepository> _repositories = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GitHubPullRequest> _pullRequests = new(StringComparer.Ordinal);
    private long _nextRepositoryId = 1;
    private long _nextPullRequestId = 1;
    private int _nextPullRequestNumber = 1;

    public bool MergeConflict { get; set; }

    public MergeMethod? UnsupportedMergeMethod { get; set; }

    public bool SourceBranchIsFromFork { get; set; }

    public Exception? CreatePullRequestFailure { get; set; }

    public Exception? DeleteBranchFailure { get; set; }

    public List<string> DeletedBranches { get; } = [];

    public int CloseCallCount { get; private set; }

    public string? LastListRepositoryOwner { get; private set; }

    public RepositoryQuery? LastRepositoryQuery { get; private set; }

    public PaginationOptions? LastRepositoryPagination { get; private set; }

    public PullRequestQuery? LastPullRequestQuery { get; private set; }

    public PaginationOptions? LastPullRequestPagination { get; private set; }

    public MergePullRequestRequest? LastMergeRequest { get; private set; }

    public Task<GitHubRepository> CreateRepositoryAsync(
        string? owner,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var resolvedOwner = owner ?? "user";
        var repository = new GitHubRepository
        {
            Id = _nextRepositoryId++,
            Name = request.Name,
            Owner = resolvedOwner,
            FullName = $"{resolvedOwner}/{request.Name}",
            HtmlUrl = $"https://github.test/{resolvedOwner}/{request.Name}",
            DefaultBranch = request.DefaultBranch ?? (request.InitializeWithReadme ? "main" : null),
            Visibility = request.Visibility,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };

        _repositories[RepositoryKey(resolvedOwner, request.Name)] = repository;
        return Task.FromResult(repository);
    }

    public Task<GitHubRepository> GetRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken)
    {
        if (!_repositories.TryGetValue(RepositoryKey(owner, name), out var repository))
        {
            throw new RepositoryNotFoundException(owner, null, name);
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
            throw new RepositoryNotFoundException(owner, null, name);
        }

        return Task.CompletedTask;
    }

    public Task<GitHubPage<GitHubRepository>> ListRepositoriesAsync(
        string? owner,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        LastListRepositoryOwner = owner;
        LastRepositoryQuery = query;
        LastRepositoryPagination = pagination;

        IEnumerable<GitHubRepository> items = _repositories.Values;
        if (!string.IsNullOrWhiteSpace(owner))
        {
            items = items.Where(repository => repository.Owner == owner);
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

        return Task.FromResult(Page(items, pagination));
    }

    public Task<GitHubPullRequest> CreatePullRequestAsync(
        string owner,
        string name,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        if (CreatePullRequestFailure is not null)
        {
            throw CreatePullRequestFailure;
        }

        var repository = GetRepositoryOrThrow(owner, name);
        var number = _nextPullRequestNumber++;
        var pullRequest = new GitHubPullRequest
        {
            Id = _nextPullRequestId++,
            Number = number,
            Title = request.Title,
            Description = request.Description,
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            State = request.Draft ? PullRequestState.Draft : PullRequestState.Open,
            IsDraft = request.Draft,
            Author = "test-user",
            HtmlUrl = $"{repository.HtmlUrl}/pull/{number}",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            HeadRepositoryFullName = repository.FullName,
            BaseRepositoryFullName = repository.FullName,
            SourceBranchIsFromFork = SourceBranchIsFromFork,
        };

        _pullRequests[PullRequestKey(repository.FullName, number)] = pullRequest;
        return Task.FromResult(pullRequest);
    }

    public Task<GitHubPullRequest> GetPullRequestAsync(
        string owner,
        string name,
        int number,
        CancellationToken cancellationToken)
    {
        var repository = GetRepositoryOrThrow(owner, name);
        return Task.FromResult(GetPullRequestOrThrow(repository.FullName, number));
    }

    public Task<GitHubPage<GitHubPullRequest>> ListPullRequestsAsync(
        string owner,
        string name,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        LastPullRequestQuery = query;
        LastPullRequestPagination = pagination;

        var repository = GetRepositoryOrThrow(owner, name);
        var prefix = repository.FullName + "#";
        IEnumerable<GitHubPullRequest> items = _pullRequests
            .Where(entry => entry.Key.StartsWith(prefix, StringComparison.Ordinal))
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

        return Task.FromResult(Page(items, pagination));
    }

    public Task<GitHubPullRequest> MergePullRequestAsync(
        string owner,
        string name,
        int number,
        MergePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        LastMergeRequest = request;

        if (UnsupportedMergeMethod == request.Method)
        {
            throw new VcsUnsupportedOperationException("github", "MergePullRequest");
        }

        if (MergeConflict)
        {
            throw new VcsConflictException("The pull request has merge conflicts.");
        }

        var repository = GetRepositoryOrThrow(owner, name);
        var existing = GetPullRequestOrThrow(repository.FullName, number);

        if (existing.State == PullRequestState.Closed)
        {
            throw new VcsConflictException($"Pull request #{number} is closed and cannot be merged.");
        }

        if (existing.State == PullRequestState.Merged)
        {
            return Task.FromResult(existing);
        }

        var merged = existing with
        {
            State = PullRequestState.Merged,
            IsDraft = false,
            MergedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };

        _pullRequests[PullRequestKey(repository.FullName, number)] = merged;
        return Task.FromResult(merged);
    }

    public Task<GitHubPullRequest> ClosePullRequestAsync(
        string owner,
        string name,
        int number,
        CancellationToken cancellationToken)
    {
        var repository = GetRepositoryOrThrow(owner, name);
        var existing = GetPullRequestOrThrow(repository.FullName, number);

        if (existing.State is PullRequestState.Closed or PullRequestState.Merged)
        {
            return Task.FromResult(existing);
        }

        CloseCallCount++;
        var closed = existing with
        {
            State = PullRequestState.Closed,
            IsDraft = false,
            ClosedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };

        _pullRequests[PullRequestKey(repository.FullName, number)] = closed;
        return Task.FromResult(closed);
    }

    public Task DeleteBranchAsync(
        string owner,
        string name,
        string branch,
        CancellationToken cancellationToken)
    {
        DeletedBranches.Add(branch);

        if (DeleteBranchFailure is not null)
        {
            throw DeleteBranchFailure;
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private GitHubRepository GetRepositoryOrThrow(string owner, string name)
    {
        if (!_repositories.TryGetValue(RepositoryKey(owner, name), out var repository))
        {
            throw new RepositoryNotFoundException(owner, null, name);
        }

        return repository;
    }

    private GitHubPullRequest GetPullRequestOrThrow(string repositoryFullName, int number)
    {
        if (!_pullRequests.TryGetValue(PullRequestKey(repositoryFullName, number), out var pullRequest))
        {
            throw new PullRequestNotFoundException(
                ParseOwner(repositoryFullName),
                null,
                ParseName(repositoryFullName),
                number);
        }

        return pullRequest;
    }

    private static GitHubPage<T> Page<T>(IEnumerable<T> items, PaginationOptions pagination)
    {
        var ordered = items.ToArray();
        var pageItems = ordered
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToArray();

        return new GitHubPage<T>(
            pageItems,
            pagination.Page * pagination.PageSize < ordered.Length);
    }

    private static string RepositoryKey(string owner, string name)
    {
        return $"{owner}/{name}";
    }

    private static string PullRequestKey(string repositoryFullName, int number)
    {
        return $"{repositoryFullName}#{number}";
    }

    private static string ParseOwner(string repositoryFullName)
    {
        var separator = repositoryFullName.IndexOf('/');
        return separator < 0 ? repositoryFullName : repositoryFullName[..separator];
    }

    private static string ParseName(string repositoryFullName)
    {
        var separator = repositoryFullName.IndexOf('/');
        return separator < 0 ? repositoryFullName : repositoryFullName[(separator + 1)..];
    }
}
