namespace VcsManager;

/// <summary>
/// Represents a repository returned by a version control provider.
/// </summary>
public record VcsRepository
{
    /// <summary>
    /// Gets the provider-specific repository identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the repository name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the owner, organization, or top-level namespace.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Gets the project or subgroup, when the provider uses one.
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    /// Gets the full provider-specific repository name.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets the repository web URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the default branch, when the repository has one.
    /// </summary>
    public string? DefaultBranch { get; init; }

    /// <summary>
    /// Gets the repository visibility.
    /// </summary>
    public VcsVisibility Visibility { get; init; } = VcsVisibility.Private;

    /// <summary>
    /// Gets the repository description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether the repository is archived.
    /// </summary>
    public bool IsArchived { get; init; }

    /// <summary>
    /// Gets the time the repository was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>
    /// Gets the time the repository was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}
