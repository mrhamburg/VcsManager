using System.Net;

namespace VcsManager;

/// <summary>
/// Retries transient provider failures with exponential backoff.
/// Merge operations are never retried because they are not automatically idempotent.
/// </summary>
public sealed class RetryLayer : IVcsProviderLayer
{
    private readonly RetryLayerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryLayer"/> class.
    /// </summary>
    /// <param name="options">The retry options, or <see langword="null"/> for defaults.</param>
    /// <exception cref="ArgumentOutOfRangeException">One of the options is outside its valid range.</exception>
    public RetryLayer(RetryLayerOptions? options = null)
    {
        _options = options ?? new RetryLayerOptions();

        if (_options.MaxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(RetryLayerOptions.MaxAttempts));
        }

        if (_options.BaseDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(RetryLayerOptions.BaseDelay));
        }

        if (_options.MaxDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(RetryLayerOptions.MaxDelay));
        }

        if (_options.JitterFactor is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(RetryLayerOptions.JitterFactor));
        }
    }

    /// <inheritdoc/>
    public IVcsProvider Wrap(IVcsProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        return new RetryProvider(inner, _options);
    }

    private sealed class RetryProvider(IVcsProvider inner, RetryLayerOptions options)
        : VcsProviderDecorator(inner)
    {
        public override Task<VcsRepository> CreateRepositoryAsync(
            CreateRepositoryRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(token => Inner.CreateRepositoryAsync(request, token), cancellationToken);
        }

        public override Task<VcsRepository> GetRepositoryAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(token => Inner.GetRepositoryAsync(repository, token), cancellationToken);
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
            return ExecuteAsync(token => Inner.DeleteRepositoryAsync(repository, token), cancellationToken);
        }

        public override Task<bool> RepositoryExistsAsync(
            RepositoryRef repository,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(token => Inner.RepositoryExistsAsync(repository, token), cancellationToken);
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
            return Inner.MergePullRequestAsync(repository, number, request, cancellationToken);
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
            return Inner.DisposeAsync();
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
            var attempt = 1;

            while (true)
            {
                try
                {
                    return await operation(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (attempt < options.MaxAttempts && ShouldRetry(exception))
                {
                    attempt++;
                    await Task.Delay(GetDelay(exception, attempt), cancellationToken);
                }
            }
        }

        private bool ShouldRetry(Exception exception)
        {
            if (exception is VcsConflictException or VcsUnsupportedOperationException)
            {
                return false;
            }

            if (exception is not VcsRateLimitException && !IsTransient(exception))
            {
                return false;
            }

            return options.RetryPredicate?.Invoke(exception) ?? true;
        }

        private static bool IsTransient(Exception exception)
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is HttpRequestException httpException &&
                    (httpException.StatusCode == HttpStatusCode.TooManyRequests ||
                     (int?)httpException.StatusCode >= 500))
                {
                    return true;
                }
            }

            return false;
        }

        private TimeSpan GetDelay(Exception exception, int attempt)
        {
            if (exception is VcsRateLimitException { RetryAfter: { } retryAfter })
            {
                return retryAfter;
            }

            var milliseconds = options.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 2);
            milliseconds = Math.Min(milliseconds, options.MaxDelay.TotalMilliseconds);

            if (options.JitterFactor > 0)
            {
                var jitter = milliseconds * options.JitterFactor * ((Random.Shared.NextDouble() * 2) - 1);
                milliseconds = Math.Max(0, milliseconds + jitter);
            }

            return TimeSpan.FromMilliseconds(milliseconds);
        }
    }
}
