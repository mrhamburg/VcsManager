namespace VcsManager;

/// <summary>
/// Represents a pull request returned by a version control provider.
/// </summary>
public record PullRequest
{
    /// <summary>
    /// Gets the provider-specific pull request identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the pull request number.
    /// </summary>
    public required int Number { get; init; }

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
    /// Gets the pull request state.
    /// </summary>
    public PullRequestState State { get; init; } = PullRequestState.Open;

    /// <summary>
    /// Gets a value indicating whether the pull request is a draft.
    /// </summary>
    public bool IsDraft { get; init; }

    /// <summary>
    /// Gets the pull request author.
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// Gets the pull request web URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the time the pull request was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>
    /// Gets the time the pull request was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Gets the time the pull request was merged.
    /// </summary>
    public DateTimeOffset? MergedAt { get; init; }

    /// <summary>
    /// Gets the time the pull request was closed.
    /// </summary>
    public DateTimeOffset? ClosedAt { get; init; }
}
