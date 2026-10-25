using System.Globalization;
using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps.Tests;

internal sealed class FakeAzureDevOpsApiClient : IAzureDevOpsApiClient
{
    public const string Organization = "conformance";

    private readonly Dictionary<string, AzureDevOpsRepository> _repositories =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, AzureDevOpsPullRequest> _pullRequests =
        new(StringComparer.Ordinal);
    private int _nextPullRequestId = 1;

    public bool MergeConflict { get; set; }

    public MergeMethod? UnsupportedMergeMethod { get; set; }

    public int CompleteCallCount { get; private set; }

    public int AbandonCallCount { get; private set; }

    public string? LastCompletedSourceCommitId { get; private set; }

    public MergePullRequestRequest? LastMergeRequest { get; private set; }

    public Task<AzureDevOpsRepository> CreateRepositoryAsync(
        string project,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var key = RepositoryKey(project, request.Name);

        if (_repositories.ContainsKey(key))
        {
            throw new VcsConflictException(
                $"Repository '{project}/{request.Name}' already exists.");
        }

        var repository = new AzureDevOpsRepository
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Project = project,
            Organization = Organization,
            WebUrl = WebUrl(project, request.Name),
            DefaultBranch = null,
            CreatedAt = DateTimeOffset.UnixEpoch,
        };

        _repositories[key] = repository;
        return Task.FromResult(repository);
    }

    public Task<AzureDevOpsRepository> GetRepositoryAsync(
        string project,
        string name,
        CancellationToken cancellationToken)
    {
        var key = RepositoryKey(project, name);

        if (!_repositories.TryGetValue(key, out var repository))
        {
            throw new RepositoryNotFoundException(Organization, project, name);
        }

        return Task.FromResult(repository);
    }

    public Task DeleteRepositoryAsync(
        string project,
        string name,
        CancellationToken cancellationToken)
    {
        var key = RepositoryKey(project, name);

        if (!_repositories.Remove(key))
        {
            throw new RepositoryNotFoundException(Organization, project, name);
        }

        return Task.CompletedTask;
    }

    public Task<AzureDevOpsPage<AzureDevOpsRepository>> ListRepositoriesAsync(
        string? project,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        IEnumerable<AzureDevOpsRepository> items = _repositories.Values;

        if (!string.IsNullOrWhiteSpace(project))
        {
            items = items.Where(repository =>
                string.Equals(repository.Project, project, StringComparison.Ordinal));
        }

        return Task.FromResult(
            Paginate(
                items.OrderBy(repository => repository.Name, StringComparer.Ordinal),
                pagination));
    }

    public Task<AzureDevOpsPullRequest> CreatePullRequestAsync(
        string project,
        string repositoryName,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        EnsureRepository(project, repositoryName);

        var number = _nextPullRequestId++;
        var pullRequest = new AzureDevOpsPullRequest
        {
            Id = number,
            Number = number,
            Title = request.Title,
            Description = request.Description,
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            State = request.Draft ? PullRequestState.Draft : PullRequestState.Open,
            IsDraft = request.Draft,
            Author = "conformance-user",
            WebUrl = $"{WebUrl(project, repositoryName)}/pullrequest/{number}",
            SourceCommitId = "2222222222222222222222222222222222222222",
            CreatedAt = DateTimeOffset.UnixEpoch,
        };

        _pullRequests[PullRequestKey(project, repositoryName, number)] = pullRequest;
        return Task.FromResult(pullRequest);
    }

    public Task<AzureDevOpsPullRequest> GetPullRequestAsync(
        string project,
        string repositoryName,
        int number,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(GetPullRequest(project, repositoryName, number));
    }

    public Task<AzureDevOpsPage<AzureDevOpsPullRequest>> ListPullRequestsAsync(
        string project,
        string repositoryName,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        EnsureRepository(project, repositoryName);

        var prefix = $"{project}|{repositoryName}|";
        IEnumerable<AzureDevOpsPullRequest> items = _pullRequests
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

        return Task.FromResult(
            Paginate(
                items.OrderBy(pullRequest => pullRequest.Number),
                pagination));
    }

    public Task<AzureDevOpsPullRequest> CompletePullRequestAsync(
        string project,
        string repositoryName,
        int number,
        string sourceCommitId,
        MergePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        CompleteCallCount++;
        LastCompletedSourceCommitId = sourceCommitId;
        LastMergeRequest = request;

        if (UnsupportedMergeMethod == request.Method)
        {
            throw new VcsUnsupportedOperationException(
                "azuredevops",
                $"MergePullRequest with MergeMethod.{request.Method}");
        }

        if (MergeConflict)
        {
            throw new VcsConflictException("The pull request cannot be completed.");
        }

        var current = GetPullRequest(project, repositoryName, number);
        var completed = current with
        {
            State = PullRequestState.Merged,
            IsDraft = false,
            MergedAt = DateTimeOffset.UnixEpoch,
        };

        _pullRequests[PullRequestKey(project, repositoryName, number)] = completed;
        return Task.FromResult(completed);
    }

    public Task<AzureDevOpsPullRequest> AbandonPullRequestAsync(
        string project,
        string repositoryName,
        int number,
        CancellationToken cancellationToken)
    {
        AbandonCallCount++;

        var current = GetPullRequest(project, repositoryName, number);
        var closed = current with
        {
            State = PullRequestState.Closed,
            IsDraft = false,
            ClosedAt = DateTimeOffset.UnixEpoch,
        };

        _pullRequests[PullRequestKey(project, repositoryName, number)] = closed;
        return Task.FromResult(closed);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private AzureDevOpsPullRequest GetPullRequest(
        string project,
        string repositoryName,
        int number)
    {
        EnsureRepository(project, repositoryName);

        if (!_pullRequests.TryGetValue(PullRequestKey(project, repositoryName, number), out var pullRequest))
        {
            throw new PullRequestNotFoundException(
                new RepositoryRef
                {
                    Owner = Organization,
                    Project = project,
                    Name = repositoryName,
                },
                number);
        }

        return pullRequest;
    }

    private void EnsureRepository(string project, string repositoryName)
    {
        if (!_repositories.ContainsKey(RepositoryKey(project, repositoryName)))
        {
            throw new RepositoryNotFoundException(Organization, project, repositoryName);
        }
    }

    private static AzureDevOpsPage<T> Paginate<T>(IEnumerable<T> source, PaginationOptions pagination)
    {
        var all = source.ToArray();
        var skip = (pagination.Page - 1) * pagination.PageSize;
        var items = all
            .Skip(skip)
            .Take(pagination.PageSize)
            .ToArray();

        return new AzureDevOpsPage<T>(
            items,
            skip + items.Length < all.Length,
            all.Length);
    }

    private static string WebUrl(string project, string name)
    {
        return $"https://dev.azure.com/{Organization}/{project}/_git/{name}";
    }

    private static string RepositoryKey(string project, string name)
    {
        return $"{project}|{name}";
    }

    private static string PullRequestKey(string project, string repositoryName, int number)
    {
        return $"{project}|{repositoryName}|{number.ToString(CultureInfo.InvariantCulture)}";
    }
}
