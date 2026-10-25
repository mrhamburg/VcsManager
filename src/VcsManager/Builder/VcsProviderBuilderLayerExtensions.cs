namespace VcsManager.Builder;

/// <summary>
/// Extension methods that add operational layers to a <see cref="VcsProviderBuilder"/>.
/// Layers are applied in registration order, so the first added layer is outermost.
/// </summary>
public static class VcsProviderBuilderLayerExtensions
{
    /// <summary>
    /// Adds a retry layer.
    /// </summary>
    public static VcsProviderBuilder WithRetry(
        this VcsProviderBuilder builder,
        RetryLayerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddLayer(new RetryLayer(options));
        return builder;
    }

    /// <summary>
    /// Adds a per-operation timeout layer.
    /// </summary>
    public static VcsProviderBuilder WithTimeout(
        this VcsProviderBuilder builder,
        TimeoutLayerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddLayer(new TimeoutLayer(options));
        return builder;
    }

    /// <summary>
    /// Adds a concurrency limiter layer.
    /// </summary>
    public static VcsProviderBuilder WithConcurrentLimiter(
        this VcsProviderBuilder builder,
        ConcurrentLimiterLayerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddLayer(new ConcurrentLimiterLayer(options));
        return builder;
    }

    /// <summary>
    /// Adds a logging layer.
    /// </summary>
    public static VcsProviderBuilder WithLogging(
        this VcsProviderBuilder builder,
        LoggingLayerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddLayer(new LoggingLayer(options));
        return builder;
    }

    /// <summary>
    /// Adds an OpenTelemetry metrics layer.
    /// </summary>
    public static VcsProviderBuilder WithOtelMetrics(
        this VcsProviderBuilder builder,
        OtelMetricsLayerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddLayer(new OtelMetricsLayer(options));
        return builder;
    }

    /// <summary>
    /// Adds an OpenTelemetry trace layer.
    /// </summary>
    public static VcsProviderBuilder WithOtelTrace(
        this VcsProviderBuilder builder,
        OtelTraceLayerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddLayer(new OtelTraceLayer(options));
        return builder;
    }
}
