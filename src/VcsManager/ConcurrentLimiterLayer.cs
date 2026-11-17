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

        protected override async Task<T> ExecuteAsync<T>(
            string operation,
            string? repository,
            int? pullRequestNumber,
            Func<CancellationToken, Task<T>> call,
            CancellationToken cancellationToken)
        {
            await AcquireAsync(cancellationToken);

            try
            {
                return await call(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override ValueTask DisposeAsync()
        {
            // The semaphore is not disposed: operations that are still running
            // must be able to release it. It holds no unmanaged resources.
            return base.DisposeAsync();
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
            catch (OperationCanceledException)
                when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                var message = $"Timed out after {acquireTimeout} waiting for a concurrency slot.";
                throw new VcsProviderException(message, new TimeoutException(message), isTransient: true);
            }
        }
    }
}
