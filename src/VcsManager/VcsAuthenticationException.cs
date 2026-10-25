namespace VcsManager;

/// <summary>
/// Thrown when a provider rejects missing or invalid credentials.
/// </summary>
public class VcsAuthenticationException : VcsProviderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VcsAuthenticationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VcsAuthenticationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VcsAuthenticationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The provider error that caused this exception.</param>
    public VcsAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
