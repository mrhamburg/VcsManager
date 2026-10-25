namespace VcsManager;

/// <summary>
/// Thrown when valid credentials do not have permission for an operation.
/// </summary>
public class VcsAuthorizationException : VcsProviderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VcsAuthorizationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VcsAuthorizationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VcsAuthorizationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The provider error that caused this exception.</param>
    public VcsAuthorizationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
