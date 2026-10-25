namespace VcsManager;

/// <summary>
/// Thrown when a provider cannot perform a requested operation.
/// </summary>
public class VcsUnsupportedOperationException : VcsProviderException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VcsUnsupportedOperationException"/> class.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="operation">The operation name.</param>
    public VcsUnsupportedOperationException(string provider, string operation)
        : base($"The {provider} provider does not support the '{operation}' operation.")
    {
        Provider = provider;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VcsUnsupportedOperationException"/> class.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="innerException">The provider error that caused this exception.</param>
    public VcsUnsupportedOperationException(
        string provider,
        string operation,
        Exception innerException)
        : base($"The {provider} provider does not support the '{operation}' operation.", innerException)
    {
        Provider = provider;
        Operation = operation;
    }

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Gets the operation name.
    /// </summary>
    public string Operation { get; }
}
