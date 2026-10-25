using VcsManager.Internal;

namespace VcsManager;

/// <summary>
/// Thrown when a pull request does not exist.
/// </summary>
public class PullRequestNotFoundException : VcsNotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestNotFoundException"/> class.
    /// </summary>
    /// <param name="repository">The repository that contains the pull request.</param>
    /// <param name="number">The pull request number.</param>
    public PullRequestNotFoundException(RepositoryRef repository, int number)
        : base("pull request", FormatMessage(repository, number))
    {
        Repository = repository;
        Number = number;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestNotFoundException"/> class.
    /// </summary>
    /// <param name="repository">The repository that contains the pull request.</param>
    /// <param name="number">The pull request number.</param>
    /// <param name="innerException">The provider error that caused this exception.</param>
    public PullRequestNotFoundException(
        RepositoryRef repository,
        int number,
        Exception innerException)
        : base("pull request", FormatMessage(repository, number), innerException)
    {
        Repository = repository;
        Number = number;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestNotFoundException"/> class.
    /// </summary>
    /// <param name="owner">The owner, organization, or namespace, when known.</param>
    /// <param name="project">The project or subgroup, when known.</param>
    /// <param name="name">The repository name.</param>
    /// <param name="number">The pull request number.</param>
    public PullRequestNotFoundException(
        string? owner,
        string? project,
        string name,
        int number)
        : this(new RepositoryRef { Owner = owner, Project = project, Name = name }, number)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PullRequestNotFoundException"/> class.
    /// </summary>
    /// <param name="owner">The owner, organization, or namespace, when known.</param>
    /// <param name="project">The project or subgroup, when known.</param>
    /// <param name="name">The repository name.</param>
    /// <param name="number">The pull request number.</param>
    /// <param name="innerException">The provider error that caused this exception.</param>
    public PullRequestNotFoundException(
        string? owner,
        string? project,
        string name,
        int number,
        Exception innerException)
        : this(new RepositoryRef { Owner = owner, Project = project, Name = name }, number, innerException)
    {
    }

    /// <summary>
    /// Gets the repository that contains the pull request.
    /// </summary>
    public RepositoryRef Repository { get; }

    /// <summary>
    /// Gets the pull request number.
    /// </summary>
    public int Number { get; }

    private static string FormatMessage(RepositoryRef repository, int number)
    {
        return $"Pull request #{number} was not found in repository '{RepositoryDisplayName.Format(repository)}'.";
    }
}
