namespace VcsManager.Gitea.Internal;

internal sealed record GiteaPullRequest
{
    public required long Id { get; init; }

    public required int Number { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required string SourceBranch { get; init; }

    public required string TargetBranch { get; init; }

    public PullRequestState State { get; init; } = PullRequestState.Open;

    public bool IsDraft { get; init; }

    public required string Author { get; init; }

    public required string WebUrl { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public DateTimeOffset? MergedAt { get; init; }

    public DateTimeOffset? ClosedAt { get; init; }
}
