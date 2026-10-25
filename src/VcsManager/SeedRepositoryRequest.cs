namespace VcsManager;

/// <summary>
/// Describes the initial commit that seeds an empty repository.
/// </summary>
public record SeedRepositoryRequest
{
    /// <summary>
    /// Gets the branch that receives the initial commit. The branch becomes the
    /// repository default branch.
    /// </summary>
    public required string Branch { get; init; }

    /// <summary>
    /// Gets the files to write in the initial commit.
    /// </summary>
    public required IReadOnlyList<RepositorySeedFile> Files { get; init; }

    /// <summary>
    /// Gets the commit message, or <see langword="null"/> for the default
    /// message <c>Initial commit</c>.
    /// </summary>
    public string? CommitMessage { get; init; }
}
