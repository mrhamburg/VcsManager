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
        protected override async Task<T> ExecuteAsync<T>(
            string operation,
            string? repository,
            int? pullRequestNumber,
            Func<CancellationToken, Task<T>> call,
            CancellationToken cancellationToken)
        {
            Task<T>? task = null;

            try
            {
                task = call(cancellationToken);

                // WaitAsync bounds the wait without disposing a linked token source
                // while the inner operation may still observe that token.
                return await task.WaitAsync(options.Timeout, cancellationToken);
            }
            catch (TimeoutException) when (task is { IsCompleted: false })
            {
                var message = $"{operation} did not complete within {options.Timeout}.";
                throw new VcsProviderException(message, new TimeoutException(message), isTransient: true);
            }
        }
    }
}
