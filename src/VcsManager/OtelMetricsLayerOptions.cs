namespace VcsManager;

/// <summary>
/// Options for <see cref="OtelMetricsLayer"/>.
/// </summary>
public sealed class OtelMetricsLayerOptions
{
    /// <summary>
    /// Gets or sets the meter name.
    /// </summary>
    public string MeterName { get; set; } = "VcsManager";

    /// <summary>
    /// Gets or sets the meter version.
    /// </summary>
    public string MeterVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the metric name prefix.
    /// </summary>
    public string MetricPrefix { get; set; } = "vcs.";

    /// <summary>
    /// Gets or sets the provider name added as a metric tag.
    /// </summary>
    public string ProviderName { get; set; } = "vcs";
}
