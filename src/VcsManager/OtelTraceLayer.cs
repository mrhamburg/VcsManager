using System.Diagnostics;
using VcsManager.Internal;

namespace VcsManager;

/// <summary>
/// Creates one activity for each provider operation and marks failures with an error status.
/// </summary>
public sealed class OtelTraceLayer : IVcsProviderLayer
{
    private readonly OtelTraceLayerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OtelTraceLayer"/> class.
    /// </summary>
    /// <param name="options">The tracing options, or <see langword="null"/> for defaults.</param>
    /// <exception cref="ArgumentException">The source or provider name is empty or whitespace.</exception>
    public OtelTraceLayer(OtelTraceLayerOptions? options = null)
    {
        _options = options ?? new OtelTraceLayerOptions();
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.ActivitySourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.ProviderName);
    }

    /// <inheritdoc/>
    public IVcsProvider Wrap(IVcsProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        return new TraceProvider(inner, _options);
    }

    private sealed class TraceProvider(IVcsProvider inner, OtelTraceLayerOptions options)
        : VcsProviderDecorator(inner)
    {
        private readonly ActivitySource _activitySource = new(
            options.ActivitySourceName,
            options.ActivitySourceVersion);
        private readonly string _providerName = options.ProviderName;
        private readonly bool _recordException = options.RecordException;

        public override Task<VcsRepository> CreateRepositoryAsync(
            CreateRepositoryRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                "CreateRepository",
                repository: null,
                pullRequestNumber: null,
                token => Inner.CreateRepositoryAsync(request, token),
                cancellationToken);
        }

        public override Task<VcsRepository> GetRepositoryAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                "GetRepository",
                RepositoryDisplayName.Format(repository),
                pullRequestNumber: null,
                token => Inner.GetRepositoryAsync(repository, token),
                cancellationToken);
        }

        public override Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
            RepositoryQuery? query = null,
            PaginationOptions? pagination = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                "ListRepositories",
                repository: null,
                pullRequestNumber: null,
                token => Inner.ListRepositoriesAsync(query, pagination, token),
                cancellationToken);
        }

        public override Task DeleteRepositoryAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                "DeleteRepository",
                RepositoryDisplayName.Format(repository),
                pullRequestNumber: null,
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
                "RepositoryExists",
                RepositoryDisplayName.Format(repository),
                pullRequestNumber: null,
                token => Inner.RepositoryExistsAsync(repository, token),
                cancellationToken);
        }

        public override Task<PullRequest> CreatePullRequestAsync(
            RepositoryRef repository,
            CreatePullRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                "CreatePullRequest",
                RepositoryDisplayName.Format(repository),
                pullRequestNumber: null,
                token => Inner.CreatePullRequestAsync(repository, request, token),
                cancellationToken);
        }

        public override Task<PullRequest> GetPullRequestAsync(
            RepositoryRef repository,
            int number,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                "GetPullRequest",
                RepositoryDisplayName.Format(repository),
                number,
                token => Inner.GetPullRequestAsync(repository, number, token),
                cancellationToken);
        }

        public override Task<PagedResult<PullRequest>> ListPullRequestsAsync(
            RepositoryRef repository,
            PullRequestQuery? query = null,
            PaginationOptions? pagination = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                "ListPullRequests",
                RepositoryDisplayName.Format(repository),
                pullRequestNumber: null,
                token => Inner.ListPullRequestsAsync(repository, query, pagination, token),
                cancellationToken);
        }

        public override Task<PullRequest> MergePullRequestAsync(
            RepositoryRef repository,
            int number,
            MergePullRequestRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                "MergePullRequest",
                RepositoryDisplayName.Format(repository),
                number,
                token => Inner.MergePullRequestAsync(repository, number, request, token),
                cancellationToken);
        }

        public override Task<PullRequest> ClosePullRequestAsync(
            RepositoryRef repository,
            int number,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                "ClosePullRequest",
                RepositoryDisplayName.Format(repository),
                number,
                token => Inner.ClosePullRequestAsync(repository, number, token),
                cancellationToken);
        }

        public override ValueTask DisposeAsync()
        {
            _activitySource.Dispose();
            return base.DisposeAsync();
        }

        private async Task<T> ExecuteAsync<T>(
            string operation,
            string? repository,
            int? pullRequestNumber,
            Func<CancellationToken, Task<T>> operationExecutor,
            CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity(operation, ActivityKind.Client);
            activity?.SetTag("vcs.provider", _providerName);
            activity?.SetTag("vcs.operation", operation);

            if (!string.IsNullOrWhiteSpace(repository))
            {
                activity?.SetTag("vcs.repository", repository);
            }

            if (pullRequestNumber is not null)
            {
                activity?.SetTag("vcs.pull_request.number", pullRequestNumber.Value);
            }

            try
            {
                var result = await operationExecutor(cancellationToken);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception exception)
            {
                activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);

                if (_recordException)
                {
                    activity?.SetTag("exception.type", exception.GetType().FullName);
                }

                throw;
            }
        }
    }
}
