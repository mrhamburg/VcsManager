namespace VcsManager;

/// <summary>
/// Limits the number of provider operations that run at the same time.
/// </summary>
public sealed class ConcurrentLimiterLayer : IVcsProviderLayer
{
    private readonly ConcurrentLimiterLayerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentLimiterLayer"/> class.
    /// </summary>
    /// <param name="options">The limiter options, or <see langword="null"/> for defaults.</param>
    /// <exception cref="ArgumentOutOfRangeException">A configured limit is outside its valid range.</exception>
    public ConcurrentLimiterLayer(ConcurrentLimiterLayerOptions? options = null)
    {
        _options = options ?? new ConcurrentLimiterLayerOptions();

        if (_options.MaxConcurrentRequests <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ConcurrentLimiterLayerOptions.MaxConcurrentRequests));
        }

        if (_options.AcquireTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ConcurrentLimiterLayerOptions.AcquireTimeout));
        }
    }

    /// <inheritdoc/>
    public IVcsProvider Wrap(IVcsProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        return new ConcurrentLimiterProvider(inner, _options);
    }

    private sealed class ConcurrentLimiterProvider : VcsProviderDecorator
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly TimeSpan? _acquireTimeout;

        public ConcurrentLimiterProvider(IVcsProvider inner, ConcurrentLimiterLayerOptions options)
            : base(inner)
        {
            _semaphore = new SemaphoreSlim(
                options.MaxConcurrentRequests,
                options.MaxConcurrentRequests);
            _acquireTimeout = options.AcquireTimeout;
        }

        public override Task<VcsRepository> CreateRepositoryAsync(
            CreateRepositoryRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.CreateRepositoryAsync(request, token),
                cancellationToken);
        }

        public override Task SeedRepositoryAsync(
            RepositoryRef repository,
            SeedRepositoryRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.SeedRepositoryAsync(repository, request, token),
                cancellationToken);
        }

        public override Task<VcsRepository> GetRepositoryAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.GetRepositoryAsync(repository, token),
                cancellationToken);
        }

        public override Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
            RepositoryQuery? query = null,
            PaginationOptions? pagination = null,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.ListRepositoriesAsync(query, pagination, token),
                cancellationToken);
        }

        public override Task DeleteRepositoryAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.DeleteRepositoryAsync(repository, token),
                cancellationToken);
        }

        public override Task<bool> RepositoryExistsAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.RepositoryExistsAsync(repository, token),
                cancellationToken);
        }

        public override Task<PullRequest> CreatePullRequestAsync(
            RepositoryRef repository,
            CreatePullRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.CreatePullRequestAsync(repository, request, token),
                cancellationToken);
        }

        public override Task<PullRequest> GetPullRequestAsync(
            RepositoryRef repository,
            int number,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
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
                token => Inner.MergePullRequestAsync(repository, number, request, token),
                cancellationToken);
        }

        public override Task<PullRequest> ClosePullRequestAsync(
            RepositoryRef repository,
            int number,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                token => Inner.ClosePullRequestAsync(repository, number, token),
                cancellationToken);
        }

        public override ValueTask DisposeAsync()
        {
            _semaphore.Dispose();
            return base.DisposeAsync();
        }

        private async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            await ExecuteAsync<object?>(
                async token =>
                {
                    await operation(token);
                    return null;
                },
                cancellationToken);
        }

        private async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            await AcquireAsync(cancellationToken);

            try
            {
                return await operation(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task AcquireAsync(CancellationToken cancellationToken)
        {
            if (_acquireTimeout is not { } acquireTimeout)
            {
                await _semaphore.WaitAsync(cancellationToken);
                return;
            }

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(acquireTimeout);

            try
            {
                await _semaphore.WaitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                var message = $"Timed out after {acquireTimeout} waiting for a concurrency slot.";
                throw new VcsProviderException(message, new TimeoutException(message), isTransient: true);
            }
        }
    }
}
