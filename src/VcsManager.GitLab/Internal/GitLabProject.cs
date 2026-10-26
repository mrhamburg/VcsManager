namespace VcsManager.GitLab.Internal;

internal sealed record GitLabProject
{
    public required long Id { get; init; }

    public required string Name { get; init; }

    public required string Path { get; init; }

    public required string PathWithNamespace { get; init; }

    public required string WebUrl { get; init; }

    public string? DefaultBranch { get; init; }

    public VcsVisibility Visibility { get; init; } = VcsVisibility.Private;

    public string? Description { get; init; }

    public bool IsArchived { get; init; }

    public bool IsEmpty { get; init; }

    public bool IsDeleted { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public string? Owner { get; init; }

    public string? Project { get; init; }
}
