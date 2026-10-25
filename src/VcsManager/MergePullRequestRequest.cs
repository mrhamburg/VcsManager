namespace VcsManager;

/// <summary>
/// Describes how to merge a pull request.
/// </summary>
public record MergePullRequestRequest
{
    /// <summary>
    /// Gets the merge method.
    /// </summary>
    public MergeMethod Method { get; init; } = MergeMethod.Merge;

    /// <summary>
    /// Gets a value indicating whether to delete the source branch after merging.
    /// </summary>
    public bool DeleteSourceBranch { get; init; }

    /// <summary>
    /// Gets the commit title.
    /// </summary>
    public string? CommitTitle { get; init; }

    /// <summary>
    /// Gets the commit message.
    /// </summary>
    public string? CommitMessage { get; init; }
}
