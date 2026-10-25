namespace VcsManager;

/// <summary>
/// Options for <see cref="OtelTraceLayer"/>.
/// </summary>
public sealed class OtelTraceLayerOptions
{
    /// <summary>
    /// Gets or sets the activity source name.
    /// </summary>
    public string ActivitySourceName { get; set; } = "VcsManager";

    /// <summary>
    /// Gets or sets the activity source version.
    /// </summary>
    public string ActivitySourceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the provider name added to activity tags.
    /// </summary>
    public string ProviderName { get; set; } = "vcs";

    /// <summary>
    /// Gets or sets a value indicating whether the failure exception type is recorded.
    /// Exception messages are never recorded.
    /// </summary>
    public bool RecordException { get; set; } = true;
}
