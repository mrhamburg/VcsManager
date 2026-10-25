namespace VcsManager;

/// <summary>
/// Options for <see cref="TimeoutLayer"/>.
/// </summary>
public sealed class TimeoutLayerOptions
{
    /// <summary>
    /// Gets or sets the timeout for each provider operation.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
