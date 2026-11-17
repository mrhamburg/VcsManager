using System.Net;
using VcsManager.Internal;

namespace VcsManager;

/// <summary>
/// Retries transient provider failures with exponential backoff.
/// Create repository, seed repository, delete repository, create pull request, and
/// merge pull request are never retried because a retry can repeat the operation.
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
        private static readonly HashSet<string> NonRetryableOperations =
            new(StringComparer.Ordinal)
            {
                VcsOperationNames.CreateRepository,
                VcsOperationNames.SeedRepository,
                VcsOperationNames.DeleteRepository,
                VcsOperationNames.CreatePullRequest,
                VcsOperationNames.MergePullRequest,
            };

        protected override Task<T> ExecuteAsync<T>(
            string operation,
            string? repository,
            int? pullRequestNumber,
            Func<CancellationToken, Task<T>> call,
            CancellationToken cancellationToken)
        {
            return NonRetryableOperations.Contains(operation)
                ? call(cancellationToken)
                : ExecuteWithRetryAsync(call, cancellationToken);
        }

        private async Task<T> ExecuteWithRetryAsync<T>(
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

            if (exception is VcsProviderException { IsTransient: true })
            {
                return options.RetryPredicate?.Invoke(exception) ?? true;
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
                return retryAfter < TimeSpan.Zero ? TimeSpan.Zero : retryAfter;
            }

            var milliseconds = options.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 2);
            milliseconds = Math.Min(milliseconds, options.MaxDelay.TotalMilliseconds);

            if (options.JitterFactor > 0)
            {
                var jitter = milliseconds * options.JitterFactor * ((Random.Shared.NextDouble() * 2) - 1);
                milliseconds = Math.Min(
                    Math.Max(0, milliseconds + jitter),
                    options.MaxDelay.TotalMilliseconds);
            }

            return TimeSpan.FromMilliseconds(milliseconds);
        }
    }
}
