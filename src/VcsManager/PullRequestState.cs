namespace VcsManager;

/// <summary>
/// Describes the current state of a pull request.
/// </summary>
public enum PullRequestState
{
    /// <summary>
    /// The pull request is open for review.
    /// </summary>
    Open,

    /// <summary>
    /// The pull request is open but marked as a draft.
    /// </summary>
    Draft,

    /// <summary>
    /// The pull request has been merged.
    /// </summary>
    Merged,

    /// <summary>
    /// The pull request has been closed without merging.
    /// </summary>
    Closed,
}
