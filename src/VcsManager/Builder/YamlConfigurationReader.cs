using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

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
    /// The document is not valid YAML, is empty, lacks a provider block, or contains
    /// more than one provider.
    /// </exception>
    public static VcsProviderConfiguration Read(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        var stream = new YamlStream();
        try
        {
            using var reader = new StringReader(yaml);
            stream.Load(reader);
        }
        catch (YamlException exception)
        {
            throw new InvalidOperationException("YAML configuration is not valid.", exception);
        }

        if (stream.Documents.Count == 0)
        {
            throw new InvalidOperationException("YAML document is empty.");
        }

        if (stream.Documents.Count != 1)
        {
            throw new InvalidOperationException(
                $"YAML must contain exactly one document. Found {stream.Documents.Count}.");
        }

        if (stream.Documents[0].RootNode is not YamlMappingNode root)
        {
            throw new InvalidOperationException("YAML document is empty.");
        }

        var providerNode = FindChild(root, "provider");
        if (providerNode is not YamlMappingNode providers)
        {
            throw new InvalidOperationException(
                "YAML must contain a top-level 'provider' key with a provider block.");
        }

        if (providers.Children.Count != 1)
        {
            throw new InvalidOperationException(
                $"Exactly one provider must be specified under 'provider'. Found {providers.Children.Count}.");
        }

        var entry = providers.Children.First();
        var providerName = GetScalar(entry.Key);
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException("A provider name is required under 'provider'.");
        }

        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (entry.Value is YamlMappingNode providerSettings)
        {
            Flatten(providerSettings, prefix: string.Empty, settings);
        }
        else if (entry.Value is not YamlScalarNode { Value: null or "" })
        {
            throw new InvalidOperationException(
                $"The settings for provider '{providerName}' must be a mapping.");
        }

        return new VcsProviderConfiguration
        {
            ProviderName = providerName,
            Settings = settings,
        };
    }

    private static void Flatten(
        YamlMappingNode source,
        string prefix,
        Dictionary<string, string> target)
    {
        foreach (var entry in source.Children)
        {
            var keyValue = GetScalar(entry.Key);
            var key = string.IsNullOrEmpty(prefix)
                ? keyValue
                : $"{prefix}.{keyValue}";

            if (entry.Value is YamlMappingNode nested)
            {
                Flatten(nested, key, target);
            }
            else if (entry.Value is YamlScalarNode { Value: { } value })
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    target[key] = value;
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"YAML setting '{key}' must be a scalar or a mapping.");
            }
        }
    }

    private static YamlNode? FindChild(YamlMappingNode node, string name)
    {
        foreach (var entry in node.Children)
        {
            if (entry.Key is YamlScalarNode { Value: { } value } &&
                string.Equals(value, name, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value;
            }
        }

        return null;
    }

    private static string GetScalar(YamlNode node)
    {
        return node is YamlScalarNode { Value: { } value } ? value : string.Empty;
    }
}
