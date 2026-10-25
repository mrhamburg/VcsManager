namespace VcsManager;

/// <summary>
/// Describes a repository to create.
/// </summary>
public record CreateRepositoryRequest
{
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
    /// Gets the repository description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the repository visibility.
    /// </summary>
    public VcsVisibility Visibility { get; init; } = VcsVisibility.Private;

    /// <summary>
    /// Gets a value indicating whether to create an initial commit with a readme file.
    /// </summary>
    public bool InitializeWithReadme { get; init; }

    /// <summary>
    /// Gets the default branch name, when the provider supports choosing one.
    /// </summary>
    public string? DefaultBranch { get; init; }
}
