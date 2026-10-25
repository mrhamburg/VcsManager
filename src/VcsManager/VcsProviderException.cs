namespace VcsManager;

/// <summary>
/// Base exception for failures reported by a version control provider.
/// </summary>
public class VcsProviderException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VcsProviderException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VcsProviderException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VcsProviderException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The provider error that caused this exception.</param>
    public VcsProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
