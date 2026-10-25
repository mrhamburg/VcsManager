using VcsManager.Builder;

namespace VcsManager.AzureDevOps;

/// <summary>
/// Creates <see cref="AzureDevOpsProvider"/> instances from flat configuration.
/// </summary>
public sealed class AzureDevOpsProviderFactory : IVcsProviderFactory
{
    /// <inheritdoc/>
    public string ProviderName => "azuredevops";

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The configuration is missing required settings.</exception>
    public IVcsProvider Create(IReadOnlyDictionary<string, string> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new AzureDevOpsProvider(MapConfiguration(configuration));
    }

    internal static AzureDevOpsOptions MapConfiguration(
        IReadOnlyDictionary<string, string> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new AzureDevOpsOptions();

        if (TryGetValue(configuration, "organization.url", out var organizationUrl))
        {
            options.OrganizationUrl = organizationUrl;
        }

        if (TryGetValue(configuration, "personal.access.token", out var token))
        {
            options.PersonalAccessToken = token;
        }

        if (TryGetValue(configuration, "default.project", out var defaultProject))
        {
            options.DefaultProject = defaultProject;
        }

        if (TryGetValue(configuration, "api.version", out var apiVersion))
        {
            options.ApiVersion = apiVersion;
        }

        return options;
    }

    private static bool TryGetValue(
        IReadOnlyDictionary<string, string> configuration,
        string key,
        out string value)
    {
        if (configuration.TryGetValue(key, out var found) && !string.IsNullOrWhiteSpace(found))
        {
            value = found.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }
}
