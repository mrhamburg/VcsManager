namespace VcsManager;

/// <summary>
/// Identifies a repository for provider operations.
/// </summary>
public record RepositoryRef
{
    /// <summary>
    /// Gets the repository name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional owner, organization, or top-level namespace.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Gets the optional project or subgroup.
    /// </summary>
    public string? Project { get; init; }
}
