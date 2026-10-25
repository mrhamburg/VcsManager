namespace VcsManager;

/// <summary>
/// Represents one file in a repository seed.
/// </summary>
public record RepositorySeedFile
{
    /// <summary>
    /// Gets the path relative to the repository root. The path uses forward
    /// slashes and must not contain parent segments or a <c>.git</c> segment.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the binary file content.
    /// </summary>
    public required ReadOnlyMemory<byte> Content { get; init; }
}
