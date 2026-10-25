using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VcsManager.Builder;

/// <summary>
/// Reads provider configuration from a YAML document.
///
/// Expected format:
/// <code>
/// provider:
///   github:
///     token: abc
///     base:
///       url: https://github.example.com
/// </code>
/// </summary>
public static class YamlConfigurationReader
{
    /// <summary>
    /// Reads the provider name and settings from YAML.
    /// </summary>
    /// <param name="yaml">The YAML content.</param>
    /// <returns>The provider configuration.</returns>
    /// <exception cref="ArgumentException"><paramref name="yaml"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// The document is empty, lacks a provider block, or contains more than one provider.
    /// </exception>
    public static VcsProviderConfiguration Read(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var root = deserializer.Deserialize<Dictionary<string, object>>(yaml)
            ?? throw new InvalidOperationException("YAML document is empty.");

        if (!root.TryGetValue("provider", out var providerObject) ||
            providerObject is not Dictionary<object, object> providers)
        {
            throw new InvalidOperationException(
                "YAML must contain a top-level 'provider' key with a provider block.");
        }

        if (providers.Count != 1)
        {
            throw new InvalidOperationException(
                $"Exactly one provider must be specified under 'provider'. Found {providers.Count}.");
        }

        var entry = providers.First();
        var providerName = entry.Key.ToString() ?? string.Empty;
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (entry.Value is Dictionary<object, object> providerSettings)
        {
            Flatten(providerSettings, prefix: string.Empty, settings);
        }

        return new VcsProviderConfiguration
        {
            ProviderName = providerName,
            Settings = settings,
        };
    }

    private static void Flatten(
        Dictionary<object, object> source,
        string prefix,
        Dictionary<string, string> target)
    {
        foreach (var entry in source)
        {
            var key = string.IsNullOrEmpty(prefix)
                ? entry.Key.ToString() ?? string.Empty
                : $"{prefix}.{entry.Key}";

            if (entry.Value is Dictionary<object, object> nested)
            {
                Flatten(nested, key, target);
            }
            else if (entry.Value is not null)
            {
                target[key] = entry.Value.ToString() ?? string.Empty;
            }
        }
    }
}
