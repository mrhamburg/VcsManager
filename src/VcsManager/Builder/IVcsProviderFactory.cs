namespace VcsManager.Builder;

/// <summary>
/// Creates a provider from a flat configuration dictionary.
/// </summary>
public interface IVcsProviderFactory
{
    /// <summary>
    /// Gets the provider name used in configuration and the builder API.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Creates a provider from the given settings.
    /// </summary>
    /// <param name="configuration">Flat provider settings with dot-separated keys.</param>
    /// <returns>The configured provider.</returns>
    IVcsProvider Create(IReadOnlyDictionary<string, string> configuration);
}
