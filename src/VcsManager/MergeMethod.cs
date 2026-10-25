namespace VcsManager;

/// <summary>
/// Describes how a pull request is merged into the target branch.
/// </summary>
public enum MergeMethod
{
    /// <summary>
    /// Merge with a merge commit.
    /// </summary>
    Merge,

    /// <summary>
    /// Combine all commits into a single commit.
    /// </summary>
    Squash,

    /// <summary>
    /// Rebase the source branch onto the target branch.
    /// </summary>
    Rebase,
}
