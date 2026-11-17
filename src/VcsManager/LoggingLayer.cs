using System.Diagnostics;

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

        protected override async Task<T> ExecuteAsync<T>(
            string operation,
            string? repository,
            int? pullRequestNumber,
            Func<CancellationToken, Task<T>> call,
            CancellationToken cancellationToken)
        {
            var context = new VcsProviderLogContext
            {
                ProviderName = _providerName,
                Operation = operation,
                Repository = repository,
                PullRequestNumber = pullRequestNumber,
            };

            var started = Stopwatch.GetTimestamp();
            _logger.LogStart(context with { Outcome = "start" });

            T result;
            try
            {
                result = await call(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogFailure(
                    context with { Outcome = "failure" },
                    Stopwatch.GetElapsedTime(started),
                    exception.GetType().Name);
                throw;
            }

            _logger.LogSuccess(
                context with { Outcome = "success" },
                Stopwatch.GetElapsedTime(started));
            return result;
        }
    }
}
