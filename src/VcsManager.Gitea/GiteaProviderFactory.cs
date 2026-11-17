using VcsManager.Builder;
using VcsManager.Internal;

namespace VcsManager.Gitea;

/// <summary>
/// Creates <see cref="GiteaProvider"/> instances from flat configuration.
/// </summary>
public sealed class GiteaProviderFactory : IVcsProviderFactory
{
    /// <inheritdoc/>
    public string ProviderName => VcsProviderNames.Gitea;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The configuration is missing required settings.</exception>
    public IVcsProvider Create(IReadOnlyDictionary<string, string> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new GiteaProvider(MapConfiguration(configuration));
    }

    internal static GiteaOptions MapConfiguration(
        IReadOnlyDictionary<string, string> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new GiteaOptions();

        if (ProviderConfiguration.TryGetValue(configuration, "token", out var token))
        {
            options.Token = token;
        }

        if (ProviderConfiguration.TryGetValue(configuration, "base.url", out var baseUrl))
        {
            options.BaseUrl = baseUrl;
        }

        if (ProviderConfiguration.TryGetValue(configuration, "default.owner", out var defaultOwner))
        {
            options.DefaultOwner = defaultOwner;
        }

        return options;
    }

}
