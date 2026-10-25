namespace VcsManager;

/// <summary>
/// Base exception for a resource that does not exist.
/// </summary>
public class VcsNotFoundException : VcsProviderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VcsNotFoundException"/> class.
    /// </summary>
    /// <param name="resource">The resource type that was not found.</param>
    public VcsNotFoundException(string resource)
        : this(resource, $"The requested {resource} was not found.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VcsNotFoundException"/> class.
    /// </summary>
    /// <param name="resource">The resource type that was not found.</param>
    /// <param name="message">The error message.</param>
    public VcsNotFoundException(string resource, string message)
        : base(message)
    {
        Resource = resource;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VcsNotFoundException"/> class.
    /// </summary>
    /// <param name="resource">The resource type that was not found.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The provider error that caused this exception.</param>
    public VcsNotFoundException(string resource, string message, Exception innerException)
        : base(message, innerException)
    {
        Resource = resource;
    }

    /// <summary>
    /// Gets the resource type that was not found.
    /// </summary>
    public string Resource { get; }
}
