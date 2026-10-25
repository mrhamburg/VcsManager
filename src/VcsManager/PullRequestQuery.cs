namespace VcsManager;

/// <summary>
/// Filters a pull request listing.
/// </summary>
public record PullRequestQuery
{
    /// <summary>
    /// Gets the state to include.
    /// </summary>
    public PullRequestState? State { get; init; }

    /// <summary>
    /// Gets the author to include.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Gets the source branch to include.
    /// </summary>
    public string? SourceBranch { get; init; }

    /// <summary>
    /// Gets the target branch to include.
    /// </summary>
    public string? TargetBranch { get; init; }
}
