using VcsManager.Internal;

namespace VcsManager;

/// <summary>
/// Thrown when a repository does not exist.
/// </summary>
public class RepositoryNotFoundException : VcsNotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryNotFoundException"/> class.
    /// </summary>
    /// <param name="owner">The owner, organization, or namespace, when known.</param>
    /// <param name="project">The project or subgroup, when known.</param>
    /// <param name="name">The repository name.</param>
    public RepositoryNotFoundException(string? owner, string? project, string name)
        : base("repository", FormatMessage(owner, project, name))
    {
        Owner = owner;
        Project = project;
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryNotFoundException"/> class.
    /// </summary>
    /// <param name="owner">The owner, organization, or namespace, when known.</param>
    /// <param name="project">The project or subgroup, when known.</param>
    /// <param name="name">The repository name.</param>
    /// <param name="innerException">The provider error that caused this exception.</param>
    public RepositoryNotFoundException(
        string? owner,
        string? project,
        string name,
        Exception innerException)
        : base("repository", FormatMessage(owner, project, name), innerException)
    {
        Owner = owner;
        Project = project;
        Name = name;
    }

    /// <summary>
    /// Gets the owner, organization, or namespace, when known.
    /// </summary>
    public string? Owner { get; }

    /// <summary>
    /// Gets the project or subgroup, when known.
    /// </summary>
    public string? Project { get; }

    /// <summary>
    /// Gets the repository name.
    /// </summary>
    public string Name { get; }

    private static string FormatMessage(string? owner, string? project, string name)
    {
        return $"Repository '{RepositoryDisplayName.Format(owner, project, name)}' was not found.";
    }
}
