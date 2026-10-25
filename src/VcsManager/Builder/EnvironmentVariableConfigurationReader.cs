using System.Collections;

namespace VcsManager.Builder;

/// <summary>
/// Reads provider configuration from environment variables.
///
/// Convention:
///   {PREFIX}_PROVIDER          → provider name (for example, "github")
///   {PREFIX}_{PROVIDER}_{KEY}  → setting key, with underscores mapping to dots for nesting
///
/// Example:
///   VCS_PROVIDER=github
///   VCS_GITHUB_TOKEN=abc
///   VCS_GITHUB_BASE_URL=https://github.example.com
///
/// Produces settings: { "token": "abc", "base.url": "https://github.example.com" }
/// </summary>
public static class EnvironmentVariableConfigurationReader
{
    /// <summary>
    /// Reads the provider name and settings from environment variables.
    /// </summary>
    /// <param name="prefix">The environment variable prefix.</param>
    /// <returns>The provider configuration.</returns>
    /// <exception cref="ArgumentException"><paramref name="prefix"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">The provider name variable is not set.</exception>
    public static VcsProviderConfiguration Read(string prefix = "VCS")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var providerName = Environment.GetEnvironmentVariable($"{prefix}_PROVIDER");
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException(
                $"Environment variable '{prefix}_PROVIDER' is not set. " +
                "It must contain the provider name (for example, 'github').");
        }

        providerName = providerName.Trim();
        var settingsPrefix = $"{prefix}_{providerName}_".ToUpperInvariant();
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var environmentKey = entry.Key.ToString();
            if (environmentKey is null ||
                !environmentKey.StartsWith(settingsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = entry.Value?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var remainder = environmentKey[settingsPrefix.Length..];
            if (string.IsNullOrWhiteSpace(remainder))
            {
                continue;
            }

            var settingKey = remainder.ToLowerInvariant().Replace('_', '.');
            settings[settingKey] = value;
        }

        return new VcsProviderConfiguration
        {
            ProviderName = providerName,
            Settings = settings,
        };
    }
}
