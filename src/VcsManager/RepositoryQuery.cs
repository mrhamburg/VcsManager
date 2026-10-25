namespace VcsManager;

/// <summary>
/// Filters a repository listing.
/// </summary>
public record RepositoryQuery
{
    /// <summary>
    /// Gets the owner, organization, or top-level namespace to list.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Gets the project or subgroup to list.
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    /// Gets the visibility to include.
    /// </summary>
    public VcsVisibility? Visibility { get; init; }

    /// <summary>
    /// Gets a value that the repository name must contain.
    /// </summary>
    public string? NameContains { get; init; }

    /// <summary>
    /// Gets a value indicating whether archived repositories are included.
    /// </summary>
    public bool IncludeArchived { get; init; }
}
