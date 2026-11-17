using System.Diagnostics;

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

        protected override async Task<T> ExecuteAsync<T>(
            string operation,
            string? repository,
            int? pullRequestNumber,
            Func<CancellationToken, Task<T>> call,
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
                var result = await call(cancellationToken);
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

        public override ValueTask DisposeAsync()
        {
            _activitySource.Dispose();
            return base.DisposeAsync();
        }
    }
}
