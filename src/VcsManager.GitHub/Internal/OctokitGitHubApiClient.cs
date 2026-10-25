using System.Globalization;
using Octokit;
using OctoPullRequest = Octokit.PullRequest;

namespace VcsManager.GitHub.Internal;

internal sealed class OctokitGitHubApiClient : IGitHubApiClient
{
    private const string ProductName = "VcsManager";

    private readonly GitHubClient _client;
    private readonly Uri _baseAddress;

    internal Uri BaseAddress => _baseAddress;

    public OctokitGitHubApiClient(GitHubOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _baseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        _client = new GitHubClient(new ProductHeaderValue(ProductName), _baseAddress)
        {
            Credentials = new Credentials(options.Token!),
        };
    }

    public async Task<GitHubRepository> CreateRepositoryAsync(
        string? owner,
        CreateRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var newRepository = new NewRepository(request.Name)
        {
            Description = request.Description,
            AutoInit = request.InitializeWithReadme ? true : null,
            Visibility = MapVisibility(request.Visibility),
        };

        var repository = owner is null
            ? await _client.Repository.Create(newRepository)
            : await _client.Repository.Create(owner, newRepository);

        if (!string.IsNullOrWhiteSpace(request.DefaultBranch) &&
            !string.IsNullOrWhiteSpace(repository.DefaultBranch) &&
            !string.Equals(repository.DefaultBranch, request.DefaultBranch, StringComparison.Ordinal))
        {
            repository = await _client.Repository.Edit(
                owner ?? repository.Owner.Login,
                request.Name,
                new RepositoryUpdate { DefaultBranch = request.DefaultBranch });
        }

        return MapRepository(repository);
    }

    public async Task<GitHubRepository> GetRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken)
    {
        return MapRepository(await _client.Repository.Get(owner, name));
    }

    public async Task DeleteRepositoryAsync(
        string owner,
        string name,
        CancellationToken cancellationToken)
    {
        await _client.Repository.Delete(owner, name);
    }

    public async Task<GitHubPage<GitHubRepository>> ListRepositoriesAsync(
        string? owner,
        RepositoryQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        var parameters = CreatePaginationParameters(pagination);

        string path;
        if (owner is null)
        {
            path = "user/repos";
            parameters["affiliation"] = "owner,collaborator,organization_member";

            if (query.Visibility is VcsVisibility.Public or VcsVisibility.Private)
            {
                parameters["visibility"] = MapVisibilityParameter(query.Visibility.Value);
            }
        }
        else
        {
            path = $"orgs/{Uri.EscapeDataString(owner)}/repos";

            if (query.Visibility is VcsVisibility.Public or VcsVisibility.Private)
            {
                parameters["type"] = MapVisibilityParameter(query.Visibility.Value);
            }
        }

        var response = await _client.Connection.Get<List<Repository>>(BuildUri(path), parameters);
        var items = response.Body.Select(MapRepository).ToArray();

        return new GitHubPage<GitHubRepository>(items, HasNextPage(response.HttpResponse));
    }

    public async Task<GitHubPullRequest> CreatePullRequestAsync(
        string owner,
        string name,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var newPullRequest = new NewPullRequest(
            request.Title,
            request.SourceBranch,
            request.TargetBranch)
        {
            Body = request.Description,
            Draft = request.Draft ? true : null,
        };

        return MapPullRequest(await _client.PullRequest.Create(owner, name, newPullRequest));
    }

    public async Task<GitHubPullRequest> GetPullRequestAsync(
        string owner,
        string name,
        int number,
        CancellationToken cancellationToken)
    {
        return MapPullRequest(await _client.PullRequest.Get(owner, name, number));
    }

    public async Task<GitHubPage<GitHubPullRequest>> ListPullRequestsAsync(
        string owner,
        string name,
        PullRequestQuery query,
        PaginationOptions pagination,
        CancellationToken cancellationToken)
    {
        var parameters = CreatePaginationParameters(pagination);
        parameters["state"] = MapStateParameter(query.State);

        if (!string.IsNullOrWhiteSpace(query.TargetBranch))
        {
            parameters["base"] = query.TargetBranch;
        }

        var path = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/pulls";
        var response = await _client.Connection.Get<List<OctoPullRequest>>(BuildUri(path), parameters);
        var items = response.Body.Select(MapPullRequest).ToArray();

        return new GitHubPage<GitHubPullRequest>(items, HasNextPage(response.HttpResponse));
    }

    public async Task<GitHubPullRequest> MergePullRequestAsync(
        string owner,
        string name,
        int number,
        MergePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var merge = new MergePullRequest
        {
            MergeMethod = MapMergeMethod(request.Method),
            CommitTitle = request.CommitTitle,
            CommitMessage = request.CommitMessage,
        };

        var result = await _client.PullRequest.Merge(owner, name, number, merge);
        if (!result.Merged)
        {
            throw new VcsConflictException(
                $"GitHub could not merge pull request #{number} in repository '{owner}/{name}': " +
                (string.IsNullOrWhiteSpace(result.Message) ? "the pull request is not mergeable." : result.Message));
        }

        return MapPullRequest(await _client.PullRequest.Get(owner, name, number));
    }

    public async Task<GitHubPullRequest> ClosePullRequestAsync(
        string owner,
        string name,
        int number,
        CancellationToken cancellationToken)
    {
        return MapPullRequest(
            await _client.PullRequest.Update(
                owner,
                name,
                number,
                new PullRequestUpdate { State = ItemState.Closed }));
    }

    public async Task DeleteBranchAsync(
        string owner,
        string name,
        string branch,
        CancellationToken cancellationToken)
    {
        await _client.Git.Reference.Delete(owner, name, $"heads/{branch}");
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private Uri BuildUri(string path)
    {
        return new Uri(_baseAddress, path);
    }

    private static Dictionary<string, string> CreatePaginationParameters(PaginationOptions pagination)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["page"] = pagination.Page.ToString(CultureInfo.InvariantCulture),
            ["per_page"] = pagination.PageSize.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static bool HasNextPage(IResponse response)
    {
        return response.ApiInfo?.Links.ContainsKey("next") ?? false;
    }

    private static string MapStateParameter(PullRequestState? state)
    {
        return state switch
        {
            PullRequestState.Open or PullRequestState.Draft => "open",
            PullRequestState.Closed or PullRequestState.Merged => "closed",
            _ => "all",
        };
    }

    private static string MapVisibilityParameter(VcsVisibility visibility)
    {
        return visibility == VcsVisibility.Public ? "public" : "private";
    }

    private static RepositoryVisibility MapVisibility(VcsVisibility visibility)
    {
        return visibility switch
        {
            VcsVisibility.Internal => RepositoryVisibility.Internal,
            VcsVisibility.Public => RepositoryVisibility.Public,
            _ => RepositoryVisibility.Private,
        };
    }

    private static PullRequestMergeMethod MapMergeMethod(MergeMethod method)
    {
        return method switch
        {
            MergeMethod.Squash => PullRequestMergeMethod.Squash,
            MergeMethod.Rebase => PullRequestMergeMethod.Rebase,
            _ => PullRequestMergeMethod.Merge,
        };
    }

    private static GitHubRepository MapRepository(Repository repository)
    {
        return new GitHubRepository
        {
            Id = repository.Id,
            Name = repository.Name,
            Owner = repository.Owner?.Login ?? string.Empty,
            FullName = repository.FullName,
            HtmlUrl = repository.HtmlUrl,
            DefaultBranch = repository.DefaultBranch,
            Visibility = MapVisibility(repository),
            Description = repository.Description,
            IsArchived = repository.Archived,
            CreatedAt = repository.CreatedAt,
            UpdatedAt = repository.UpdatedAt,
        };
    }

    private static VcsVisibility MapVisibility(Repository repository)
    {
        if (repository.Visibility is { } visibility)
        {
            return visibility switch
            {
                RepositoryVisibility.Internal => VcsVisibility.Internal,
                RepositoryVisibility.Public => VcsVisibility.Public,
                _ => VcsVisibility.Private,
            };
        }

        return repository.Private ? VcsVisibility.Private : VcsVisibility.Public;
    }

    private static GitHubPullRequest MapPullRequest(OctoPullRequest pullRequest)
    {
        return new GitHubPullRequest
        {
            Id = pullRequest.Id,
            Number = pullRequest.Number,
            Title = pullRequest.Title,
            Description = pullRequest.Body,
            SourceBranch = pullRequest.Head.Ref,
            TargetBranch = pullRequest.Base.Ref,
            State = MapPullRequestState(pullRequest),
            IsDraft = pullRequest.Draft,
            Author = pullRequest.User?.Login ?? string.Empty,
            HtmlUrl = pullRequest.HtmlUrl,
            CreatedAt = pullRequest.CreatedAt,
            UpdatedAt = pullRequest.UpdatedAt,
            MergedAt = pullRequest.MergedAt,
            ClosedAt = pullRequest.ClosedAt,
            SourceBranchIsFromFork = IsFromFork(pullRequest),
        };
    }

    private static PullRequestState MapPullRequestState(OctoPullRequest pullRequest)
    {
        if (pullRequest.Merged || pullRequest.MergedAt is not null)
        {
            return PullRequestState.Merged;
        }

        if (pullRequest.State == ItemState.Closed)
        {
            return PullRequestState.Closed;
        }

        return pullRequest.Draft ? PullRequestState.Draft : PullRequestState.Open;
    }

    internal static bool IsFromFork(OctoPullRequest pullRequest)
    {
        if (pullRequest.Head?.Repository is not { } headRepository)
        {
            return true;
        }

        if (pullRequest.Base?.Repository is { } baseRepository)
        {
            return !string.Equals(
                headRepository.FullName,
                baseRepository.FullName,
                StringComparison.OrdinalIgnoreCase);
        }

        return headRepository.Fork;
    }
}
