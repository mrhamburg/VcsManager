using System.Globalization;
using VcsManager.GitLab.Internal;

namespace VcsManager.GitLab.Tests;

internal sealed class FakeGitLabApiClient : IGitLabApiClient
{
    private readonly Dictionary<string, GitLabProject> _projects = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GitLabMergeRequest> _mergeRequests = new(StringComparer.Ordinal);
    private long _nextProjectId = 1;
    private long _nextMergeRequestId = 1;
    private int _nextMergeRequestIid = 1;

    public bool MergeConflict { get; set; }

    public int CloseCallCount { get; private set; }

    public string? LastListNamespace { get; private set; }

    public RepositoryQuery? LastRepositoryQuery { get; private set; }

    public PaginationOptions? LastRepositoryPagination { get; private set; }

    public PullRequestQuery? LastPullRequestQuery { get; private set; }

    public PaginationOptions? LastPullRequestPagination { get; private set; }

    public MergePullRequestRequest? LastMergeRequest { get; private set; }

    public Task<GitLabProject> CreateProjectAsync(
        string? @namespace,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var path = GitLabProjectPath.Combine(@namespace, null, request.Name);
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var owner = segments.Length > 1 ? segments[0] : null;
        var project = segments.Length > 2
            ? string.Join("/", segments[1..^1])
            : null;

        var created = new GitLabProject
        {
            Id = _nextProjectId++,
            Name = request.Name,
            Path = request.Name,
            PathWithNamespace = path,
            WebUrl = $"https://gitlab.test/{path}",
            DefaultBranch = request.DefaultBranch ?? (request.InitializeWithReadme ? "main" : null),
            Visibility = request.Visibility,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            Owner = owner,
            Project = project,
        };

        _projects[path] = created;
        return Task.FromResult(created);
    }

    public Task<GitLabProject> GetProjectAsync(
        string projectPath,
        CancellationToken cancellationToken)
    {
        if (!_projects.TryGetValue(projectPath, out var project))
        {
            throw new RepositoryNotFoundException(
                OwnerOf(projectPath),
                ProjectOf(projectPath),
                NameOf(projectPath));
        }

        return Task.FromResult(project);
    }

    public Task DeleteProjectAsync(
        string projectPath,
        CancellationToken cancellationToken)
    {
        if (!_projects.Remove(projectPath))
        {
            throw new RepositoryNotFoundException(
                OwnerOf(projectPath),
                ProjectOf(projectPath),
                NameOf(projectPath));
        }

        return Task.CompletedTask;
    }

    public Task<GitLabPage<GitLabProject>> ListProjectsAsync(
        string? @namespace,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        LastListNamespace = @namespace;
        LastRepositoryQuery = query;
        LastRepositoryPagination = pagination;

        IEnumerable<GitLabProject> items = _projects.Values;

        if (!string.IsNullOrWhiteSpace(@namespace))
        {
            var prefix = @namespace.TrimEnd('/') + "/";
            items = items.Where(project =>
                project.PathWithNamespace.StartsWith(prefix, StringComparison.Ordinal));
        }

        if (!query.IncludeArchived)
        {
            items = items.Where(project => !project.IsArchived);
        }

        if (query.Visibility is { } visibility)
        {
            items = items.Where(project => project.Visibility == visibility);
        }

        if (!string.IsNullOrWhiteSpace(query.NameContains))
        {
            items = items.Where(project =>
                project.Path.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(
            Paginate(
                items.OrderBy(project => project.PathWithNamespace, StringComparer.Ordinal),
                pagination,
                items => items));
    }

    public Task<GitLabMergeRequest> CreateMergeRequestAsync(
        string projectPath,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        EnsureProject(projectPath);

        var iid = _nextMergeRequestIid++;
        var mergeRequest = new GitLabMergeRequest
        {
            Id = _nextMergeRequestId++,
            Iid = iid,
            Title = GitLabDraftTitle.Apply(request.Title, request.Draft),
            Description = request.Description,
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            State = request.Draft ? PullRequestState.Draft : PullRequestState.Open,
            IsDraft = request.Draft,
            Author = "test-user",
            WebUrl = $"https://gitlab.test/{projectPath}/-/merge_requests/{iid}",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };

        _mergeRequests[MergeRequestKey(projectPath, iid)] = mergeRequest;
        return Task.FromResult(mergeRequest);
    }

    public Task<GitLabMergeRequest> GetMergeRequestAsync(
        string projectPath,
        int iid,
        CancellationToken cancellationToken)
    {
        EnsureProject(projectPath);

        if (!_mergeRequests.TryGetValue(MergeRequestKey(projectPath, iid), out var mergeRequest))
        {
            throw new PullRequestNotFoundException(
                CreateRepositoryRef(projectPath),
                iid);
        }

        return Task.FromResult(mergeRequest);
    }

    public Task<GitLabPage<GitLabMergeRequest>> ListMergeRequestsAsync(
        string projectPath,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        EnsureProject(projectPath);
        LastPullRequestQuery = query;
        LastPullRequestPagination = pagination;

        var prefix = projectPath + "|";
        IEnumerable<GitLabMergeRequest> items = _mergeRequests
            .Where(entry => entry.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(entry => entry.Value);

        if (query.State is { } state)
        {
            items = items.Where(mergeRequest => mergeRequest.State == state);
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            items = items.Where(mergeRequest =>
                string.Equals(mergeRequest.Author, query.Author, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.SourceBranch))
        {
            items = items.Where(mergeRequest =>
                string.Equals(mergeRequest.SourceBranch, query.SourceBranch, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(query.TargetBranch))
        {
            items = items.Where(mergeRequest =>
                string.Equals(mergeRequest.TargetBranch, query.TargetBranch, StringComparison.Ordinal));
        }

        return Task.FromResult(
            Paginate(
                items.OrderBy(mergeRequest => mergeRequest.Iid),
                pagination,
                items => items));
    }

    public async Task<GitLabMergeRequest> MergeMergeRequestAsync(
        string projectPath,
        int iid,
        MergePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        EnsureProject(projectPath);
        LastMergeRequest = request;

        if (MergeConflict)
        {
            throw new VcsConflictException("The merge request cannot be merged.");
        }

        var current = await GetMergeRequestAsync(projectPath, iid, cancellationToken);
        var merged = current with
        {
            State = PullRequestState.Merged,
            IsDraft = false,
            MergedAt = DateTimeOffset.UnixEpoch,
        };

        _mergeRequests[MergeRequestKey(projectPath, iid)] = merged;
        return merged;
    }

    public async Task<GitLabMergeRequest> CloseMergeRequestAsync(
        string projectPath,
        int iid,
        CancellationToken cancellationToken)
    {
        EnsureProject(projectPath);
        CloseCallCount++;

        var current = await GetMergeRequestAsync(projectPath, iid, cancellationToken);
        var closed = current with
        {
            State = PullRequestState.Closed,
            IsDraft = false,
            ClosedAt = DateTimeOffset.UnixEpoch,
        };

        _mergeRequests[MergeRequestKey(projectPath, iid)] = closed;
        return closed;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private GitLabPage<T> Paginate<T>(
        IEnumerable<T> source,
        PaginationOptions pagination,
        Func<IEnumerable<T>, IEnumerable<T>> selector)
    {
        var all = selector(source).ToArray();
        var skip = (pagination.Page - 1) * pagination.PageSize;
        var items = all
            .Skip(skip)
            .Take(pagination.PageSize)
            .ToArray();

        return new GitLabPage<T>(
            items,
            skip + items.Length < all.Length,
            all.Length);
    }

    private void EnsureProject(string projectPath)
    {
        if (!_projects.ContainsKey(projectPath))
        {
            throw new RepositoryNotFoundException(
                OwnerOf(projectPath),
                ProjectOf(projectPath),
                NameOf(projectPath));
        }
    }

    private static string MergeRequestKey(string projectPath, int iid)
    {
        return $"{projectPath}|{iid.ToString(CultureInfo.InvariantCulture)}";
    }

    private static RepositoryRef CreateRepositoryRef(string projectPath)
    {
        return new RepositoryRef
        {
            Owner = OwnerOf(projectPath),
            Project = ProjectOf(projectPath),
            Name = NameOf(projectPath),
        };
    }

    private static string? OwnerOf(string projectPath)
    {
        var segments = projectPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 1 ? segments[0] : null;
    }

    private static string? ProjectOf(string projectPath)
    {
        var segments = projectPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 2 ? string.Join("/", segments[1..^1]) : null;
    }

    private static string NameOf(string projectPath)
    {
        var segments = projectPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 ? projectPath : segments[^1];
    }
}
