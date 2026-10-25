using System.Diagnostics;
using System.Diagnostics.Metrics;
using VcsManager.Internal;

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

        public override Task SeedRepositoryAsync(
            RepositoryRef repository,
            SeedRepositoryRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                "SeedRepository",
                RepositoryDisplayName.Format(repository),
                pullRequestNumber: null,
                async token =>
                {
                    await Inner.SeedRepositoryAsync(repository, request, token);
                    return true;
                },
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
            _meter.Dispose();
            return base.DisposeAsync();
        }

        private async Task<T> ExecuteAsync<T>(
            string operation,
            string? repository,
            int? pullRequestNumber,
            Func<CancellationToken, Task<T>> operationExecutor,
            CancellationToken cancellationToken)
        {
            var tags = CreateTags(operation, repository, pullRequestNumber);
            _callCounter.Add(1, tags);
            var started = Stopwatch.GetTimestamp();

            try
            {
                var result = await operationExecutor(cancellationToken);
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
