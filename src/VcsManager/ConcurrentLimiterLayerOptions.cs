namespace VcsManager;

/// <summary>
/// Options for <see cref="ConcurrentLimiterLayer"/>.
/// </summary>
public sealed class ConcurrentLimiterLayerOptions
{
    /// <summary>
    /// Gets or sets the maximum number of concurrent operations.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Gets or sets the optional timeout for acquiring a concurrency slot.
    /// When <see langword="null"/>, the layer waits indefinitely.
    /// </summary>
    public TimeSpan? AcquireTimeout { get; set; }
}
