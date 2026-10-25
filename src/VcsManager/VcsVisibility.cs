namespace VcsManager;

/// <summary>
/// Describes who can see a repository.
/// </summary>
public enum VcsVisibility
{
    /// <summary>
    /// The repository is visible only to authorized users.
    /// </summary>
    Private,

    /// <summary>
    /// The repository is visible to authenticated users of the provider.
    /// </summary>
    Internal,

    /// <summary>
    /// The repository is visible to everyone.
    /// </summary>
    Public,
}
