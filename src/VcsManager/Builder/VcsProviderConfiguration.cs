namespace VcsManager.Builder;

/// <summary>
/// Holds the selected provider name and its flat settings.
/// </summary>
public sealed class VcsProviderConfiguration
{
    /// <summary>
    /// Gets or sets the selected provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider settings. Keys are case-insensitive.
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
