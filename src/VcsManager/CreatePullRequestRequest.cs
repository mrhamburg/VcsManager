namespace VcsManager;

/// <summary>
/// Describes a pull request to create.
/// </summary>
public record CreatePullRequestRequest
{
    /// <summary>
    /// Gets the pull request title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the pull request description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the source branch name.
    /// </summary>
    public required string SourceBranch { get; init; }

    /// <summary>
    /// Gets the target branch name.
    /// </summary>
    public required string TargetBranch { get; init; }

    /// <summary>
    /// Gets a value indicating whether to create the pull request as a draft.
    /// </summary>
    public bool Draft { get; init; }
}
