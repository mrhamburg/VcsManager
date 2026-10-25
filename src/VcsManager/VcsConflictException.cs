namespace VcsManager;

/// <summary>
/// Thrown when an operation conflicts with the current state of a resource.
/// </summary>
public class VcsConflictException : VcsProviderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VcsConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VcsConflictException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VcsConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The provider error that caused this exception.</param>
    public VcsConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
