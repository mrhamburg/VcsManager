using VcsManager.Builder;
using VcsManager.Internal;

namespace VcsManager.AzureDevOps;

/// <summary>
/// Creates <see cref="AzureDevOpsProvider"/> instances from flat configuration.
/// </summary>
public sealed class AzureDevOpsProviderFactory : IVcsProviderFactory
{
    /// <inheritdoc/>
    public string ProviderName => VcsProviderNames.AzureDevOps;

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

        if (ProviderConfiguration.TryGetValue(configuration, "organization.url", out var organizationUrl))
        {
            options.OrganizationUrl = organizationUrl;
        }

        if (ProviderConfiguration.TryGetValue(configuration, "personal.access.token", out var token))
        {
            options.PersonalAccessToken = token;
        }

        if (ProviderConfiguration.TryGetValue(configuration, "default.project", out var defaultProject))
        {
            options.DefaultProject = defaultProject;
        }

        if (ProviderConfiguration.TryGetValue(configuration, "api.version", out var apiVersion))
        {
            options.ApiVersion = apiVersion;
        }

        return options;
    }

}
