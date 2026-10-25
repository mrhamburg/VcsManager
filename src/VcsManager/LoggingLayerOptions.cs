namespace VcsManager;

/// <summary>
/// Options for <see cref="LoggingLayer"/>.
/// </summary>
public sealed class LoggingLayerOptions
{
    /// <summary>
    /// Gets or sets the provider name added to log events.
    /// </summary>
    public string ProviderName { get; set; } = "vcs";

    /// <summary>
    /// Gets or sets the log sink. Defaults to a no-op logger.
    /// </summary>
    public IVcsProviderLogger Logger { get; set; } = new NullVcsProviderLogger();
}

/// <summary>
/// Receives safe provider operation events.
/// </summary>
public interface IVcsProviderLogger
{
    /// <summary>
    /// Called before an operation starts.
    /// </summary>
    void LogStart(VcsProviderLogContext context);

    /// <summary>
    /// Called after an operation succeeds.
    /// </summary>
    void LogSuccess(VcsProviderLogContext context, TimeSpan duration);

    /// <summary>
    /// Called after an operation fails.
    /// </summary>
    void LogFailure(VcsProviderLogContext context, TimeSpan duration, string exceptionType);
}

/// <summary>
/// Safe fields for a provider operation log event. It never contains request
/// bodies, settings, tokens, or authorization headers.
/// </summary>
public sealed record VcsProviderLogContext
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the operation name.
    /// </summary>
    public string Operation { get; init; } = string.Empty;

    /// <summary>
    /// Gets the repository display name, when applicable.
    /// </summary>
    public string? Repository { get; init; }

    /// <summary>
    /// Gets the pull request number, when applicable.
    /// </summary>
    public int? PullRequestNumber { get; init; }

    /// <summary>
    /// Gets the outcome: start, success, or failure.
    /// </summary>
    public string Outcome { get; init; } = string.Empty;
}

/// <summary>
/// Logger that ignores every event.
/// </summary>
public sealed class NullVcsProviderLogger : IVcsProviderLogger
{
    /// <inheritdoc/>
    public void LogStart(VcsProviderLogContext context)
    {
    }

    /// <inheritdoc/>
    public void LogSuccess(VcsProviderLogContext context, TimeSpan duration)
    {
    }

    /// <inheritdoc/>
    public void LogFailure(VcsProviderLogContext context, TimeSpan duration, string exceptionType)
    {
    }
}
