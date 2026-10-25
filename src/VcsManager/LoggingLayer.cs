using System.Diagnostics;
using VcsManager.Internal;

namespace VcsManager;

/// <summary>
/// Emits start, success, and failure events for provider operations.
/// Events contain only safe fields and never include tokens, authorization
/// headers, request bodies, or exception messages.
/// </summary>
public sealed class LoggingLayer : IVcsProviderLayer
{
    private readonly LoggingLayerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingLayer"/> class.
    /// </summary>
    /// <param name="options">The logging options, or <see langword="null"/> for defaults.</param>
    /// <exception cref="ArgumentNullException">The logger is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The provider name is empty or whitespace.</exception>
    public LoggingLayer(LoggingLayerOptions? options = null)
    {
        _options = options ?? new LoggingLayerOptions();
        ArgumentNullException.ThrowIfNull(_options.Logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.ProviderName);
    }

    /// <inheritdoc/>
    public IVcsProvider Wrap(IVcsProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        return new LoggingProvider(inner, _options);
    }

    private sealed class LoggingProvider(IVcsProvider inner, LoggingLayerOptions options)
        : VcsProviderDecorator(inner)
    {
        private readonly IVcsProviderLogger _logger = options.Logger;
        private readonly string _providerName = options.ProviderName;

        public override Task<VcsRepository> CreateRepositoryAsync(
            CreateRepositoryRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                Context(
                    "CreateRepository",
                    RepositoryDisplayName.Format(request.Owner, request.Project, request.Name)),
                token => Inner.CreateRepositoryAsync(request, token), cancellationToken);
        }

        public override Task<VcsRepository> GetRepositoryAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                Context("GetRepository", RepositoryDisplayName.Format(repository)),
                token => Inner.GetRepositoryAsync(repository, token), cancellationToken);
        }

        public override Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
            RepositoryQuery? query = null,
            PaginationOptions? pagination = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                Context("ListRepositories", FormatScope(query?.Owner, query?.Project)),
                token => Inner.ListRepositoriesAsync(query, pagination, token), cancellationToken);
        }

        public override Task DeleteRepositoryAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                Context("DeleteRepository", RepositoryDisplayName.Format(repository)),
                async token =>
                {
                    await Inner.DeleteRepositoryAsync(repository, token);
                    return true;
                },
                cancellationToken);
        }

        public override Task<bool> RepositoryExistsAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                Context("RepositoryExists", RepositoryDisplayName.Format(repository)),
                token => Inner.RepositoryExistsAsync(repository, token), cancellationToken);
        }

        public override Task<PullRequest> CreatePullRequestAsync(
            RepositoryRef repository,
            CreatePullRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                Context("CreatePullRequest", RepositoryDisplayName.Format(repository)),
                token => Inner.CreatePullRequestAsync(repository, request, token), cancellationToken);
        }

        public override Task<PullRequest> GetPullRequestAsync(
            RepositoryRef repository,
            int number,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                Context("GetPullRequest", RepositoryDisplayName.Format(repository), number),
                token => Inner.GetPullRequestAsync(repository, number, token), cancellationToken);
        }

        public override Task<PagedResult<PullRequest>> ListPullRequestsAsync(
            RepositoryRef repository,
            PullRequestQuery? query = null,
            PaginationOptions? pagination = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                Context("ListPullRequests", RepositoryDisplayName.Format(repository)),
                token => Inner.ListPullRequestsAsync(repository, query, pagination, token), cancellationToken);
        }

        public override Task<PullRequest> MergePullRequestAsync(
            RepositoryRef repository,
            int number,
            MergePullRequestRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                Context("MergePullRequest", RepositoryDisplayName.Format(repository), number),
                token => Inner.MergePullRequestAsync(repository, number, request, token), cancellationToken);
        }

        public override Task<PullRequest> ClosePullRequestAsync(
            RepositoryRef repository,
            int number,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                Context("ClosePullRequest", RepositoryDisplayName.Format(repository), number),
                token => Inner.ClosePullRequestAsync(repository, number, token), cancellationToken);
        }

        public override ValueTask DisposeAsync()
        {
            return Inner.DisposeAsync();
        }

        private VcsProviderLogContext Context(
            string operation,
            string? repository = null,
            int? pullRequestNumber = null)
        {
            return new VcsProviderLogContext
            {
                ProviderName = _providerName,
                Operation = operation,
                Repository = repository,
                PullRequestNumber = pullRequestNumber,
            };
        }

        private async Task<T> ExecuteAsync<T>(
            VcsProviderLogContext context,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            var started = Stopwatch.GetTimestamp();
            _logger.LogStart(context with { Outcome = "start" });

            try
            {
                var result = await operation(cancellationToken);
                _logger.LogSuccess(
                    context with { Outcome = "success" },
                    Stopwatch.GetElapsedTime(started));
                return result;
            }
            catch (Exception exception)
            {
                _logger.LogFailure(
                    context with { Outcome = "failure" },
                    Stopwatch.GetElapsedTime(started),
                    exception.GetType().Name);
                throw;
            }
        }

        private static string? FormatScope(string? owner, string? project)
        {
            if (string.IsNullOrWhiteSpace(owner) && string.IsNullOrWhiteSpace(project))
            {
                return null;
            }

            var parts = new List<string>(2);
            if (!string.IsNullOrWhiteSpace(owner))
            {
                parts.Add(owner);
            }

            if (!string.IsNullOrWhiteSpace(project))
            {
                parts.Add(project);
            }

            return string.Join("/", parts);
        }
    }
}
