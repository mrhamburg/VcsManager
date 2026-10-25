namespace VcsManager.Gitea.Internal;

internal sealed record GiteaRepository
{
    public required long Id { get; init; }

    public required string Name { get; init; }

    public required string Owner { get; init; }

    public required string FullName { get; init; }

    public required string WebUrl { get; init; }

    public string? DefaultBranch { get; init; }

    public VcsVisibility Visibility { get; init; } = VcsVisibility.Private;

    public string? Description { get; init; }

    public bool IsArchived { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
