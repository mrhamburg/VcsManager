using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace VcsManager;

/// <summary>
/// Emits operation count, failure count, and duration metrics for provider operations.
/// </summary>
public sealed class OtelMetricsLayer : IVcsProviderLayer
{
    private readonly OtelMetricsLayerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OtelMetricsLayer"/> class.
    /// </summary>
    /// <param name="options">The metrics options, or <see langword="null"/> for defaults.</param>
    /// <exception cref="ArgumentException">The meter or provider name is empty or whitespace.</exception>
    public OtelMetricsLayer(OtelMetricsLayerOptions? options = null)
    {
        _options = options ?? new OtelMetricsLayerOptions();
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.MeterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.ProviderName);
    }

    /// <inheritdoc/>
    public IVcsProvider Wrap(IVcsProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        return new MetricsProvider(inner, _options);
    }

    private sealed class MetricsProvider : VcsProviderDecorator
    {
        private readonly Meter _meter;
        private readonly Counter<long> _callCounter;
        private readonly Counter<long> _failureCounter;
        private readonly Histogram<double> _duration;
        private readonly string _providerName;

        public MetricsProvider(IVcsProvider inner, OtelMetricsLayerOptions options)
            : base(inner)
        {
            _meter = new Meter(options.MeterName, options.MeterVersion);
            _providerName = options.ProviderName;
            var prefix = NormalizePrefix(options.MetricPrefix);

            _callCounter = _meter.CreateCounter<long>(
                name: $"{prefix}operation.calls",
                unit: "operations",
                description: "Number of provider operations.");

            _failureCounter = _meter.CreateCounter<long>(
                name: $"{prefix}operation.failures",
                unit: "failures",
                description: "Number of failed provider operations.");

            _duration = _meter.CreateHistogram<double>(
                name: $"{prefix}operation.duration_ms",
                unit: "ms",
                description: "Provider operation duration in milliseconds.");
        }

        protected override async Task<T> ExecuteAsync<T>(
            string operation,
            string? repository,
            int? pullRequestNumber,
            Func<CancellationToken, Task<T>> call,
            CancellationToken cancellationToken)
        {
            var tags = CreateTags(operation, repository, pullRequestNumber);
            _callCounter.Add(1, tags);
            var started = Stopwatch.GetTimestamp();

            try
            {
                var result = await call(cancellationToken);
                var successTags = tags;
                successTags.Add("outcome", "success");
                _duration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, successTags);
                return result;
            }
            catch
            {
                var failureTags = tags;
                failureTags.Add("outcome", "failure");
                _failureCounter.Add(1, failureTags);
                _duration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, failureTags);
                throw;
            }
        }

        public override ValueTask DisposeAsync()
        {
            _meter.Dispose();
            return base.DisposeAsync();
        }

        private TagList CreateTags(string operation, string? repository, int? pullRequestNumber)
        {
            var tags = new TagList
            {
                { "provider", _providerName },
                { "operation", operation },
            };

            if (!string.IsNullOrWhiteSpace(repository))
            {
                tags.Add("repository", repository);
            }

            if (pullRequestNumber is not null)
            {
                tags.Add("pull_request", pullRequestNumber.Value);
            }

            return tags;
        }

        private static string NormalizePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return string.Empty;
            }

            return prefix.EndsWith('.') ? prefix : prefix + ".";
        }
    }
}
