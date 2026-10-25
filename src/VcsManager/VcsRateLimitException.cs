namespace VcsManager;

/// <summary>
/// Thrown when a provider rate limit is exceeded.
/// </summary>
public class VcsRateLimitException : VcsProviderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VcsRateLimitException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="retryAfter">The delay before the caller can retry, when the provider supplies one.</param>
    public VcsRateLimitException(string message, TimeSpan? retryAfter = null)
        : base(message)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VcsRateLimitException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="retryAfter">The delay before the caller can retry, when the provider supplies one.</param>
    /// <param name="innerException">The provider error that caused this exception.</param>
    public VcsRateLimitException(string message, TimeSpan? retryAfter, Exception innerException)
        : base(message, innerException)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Gets the delay before the caller can retry, when the provider supplies one.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
