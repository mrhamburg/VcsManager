namespace VcsManager;

/// <summary>
/// Options for <see cref="RetryLayer"/>.
/// </summary>
public sealed class RetryLayerOptions
{
    /// <summary>
    /// Gets or sets the maximum number of attempts, including the initial attempt.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay between attempts.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the maximum delay between attempts.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the jitter factor between 0 and 1 applied to the delay.
    /// </summary>
    public double JitterFactor { get; set; } = 0.2;

    /// <summary>
    /// Gets or sets an optional predicate that can veto a retry.
    /// It cannot make a non-retryable failure retryable.
    /// </summary>
    public Func<Exception, bool>? RetryPredicate { get; set; }
}
