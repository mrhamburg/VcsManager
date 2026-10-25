namespace VcsManager;

/// <summary>
/// Enforces a timeout on each provider operation.
/// </summary>
public sealed class TimeoutLayer : IVcsProviderLayer
{
    private readonly TimeoutLayerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutLayer"/> class.
    /// </summary>
    /// <param name="options">The timeout options, or <see langword="null"/> for defaults.</param>
    /// <exception cref="ArgumentOutOfRangeException">The timeout is not positive.</exception>
    public TimeoutLayer(TimeoutLayerOptions? options = null)
    {
        _options = options ?? new TimeoutLayerOptions();

        if (_options.Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(TimeoutLayerOptions.Timeout));
        }
    }

    /// <inheritdoc/>
    public IVcsProvider Wrap(IVcsProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        return new TimeoutProvider(inner, _options);
    }

    private sealed class TimeoutProvider(IVcsProvider inner, TimeoutLayerOptions options)
        : VcsProviderDecorator(inner)
    {
        public override Task<VcsRepository> CreateRepositoryAsync(
            CreateRepositoryRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.CreateRepositoryAsync(request, token),
                "CreateRepository",
                cancellationToken);
        }

        public override Task<VcsRepository> GetRepositoryAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.GetRepositoryAsync(repository, token),
                "GetRepository",
                cancellationToken);
        }

        public override Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
            RepositoryQuery? query = null,
            PaginationOptions? pagination = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.ListRepositoriesAsync(query, pagination, token),
                "ListRepositories",
                cancellationToken);
        }

        public override Task DeleteRepositoryAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.DeleteRepositoryAsync(repository, token),
                "DeleteRepository",
                cancellationToken);
        }

        public override Task<bool> RepositoryExistsAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.RepositoryExistsAsync(repository, token),
                "RepositoryExists",
                cancellationToken);
        }

        public override Task<PullRequest> CreatePullRequestAsync(
            RepositoryRef repository,
            CreatePullRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.CreatePullRequestAsync(repository, request, token),
                "CreatePullRequest",
                cancellationToken);
        }

        public override Task<PullRequest> GetPullRequestAsync(
            RepositoryRef repository,
            int number,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.GetPullRequestAsync(repository, number, token),
                "GetPullRequest",
                cancellationToken);
        }

        public override Task<PagedResult<PullRequest>> ListPullRequestsAsync(
            RepositoryRef repository,
            PullRequestQuery? query = null,
            PaginationOptions? pagination = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.ListPullRequestsAsync(repository, query, pagination, token),
                "ListPullRequests",
                cancellationToken);
        }

        public override Task<PullRequest> MergePullRequestAsync(
            RepositoryRef repository,
            int number,
            MergePullRequestRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.MergePullRequestAsync(repository, number, request, token),
                "MergePullRequest",
                cancellationToken);
        }

        public override Task<PullRequest> ClosePullRequestAsync(
            RepositoryRef repository,
            int number,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.ClosePullRequestAsync(repository, number, token),
                "ClosePullRequest",
                cancellationToken);
        }

        public override ValueTask DisposeAsync()
        {
            return Inner.DisposeAsync();
        }

        private async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            string operationName,
            CancellationToken cancellationToken)
        {
            await ExecuteAsync<object?>(
                async token =>
                {
                    await operation(token);
                    return null;
                },
                operationName,
                cancellationToken);
        }

        private async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            string operationName,
            CancellationToken cancellationToken)
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(options.Timeout);

            try
            {
                return await operation(timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"{operationName} did not complete within {options.Timeout}.");
            }
        }
    }
}
