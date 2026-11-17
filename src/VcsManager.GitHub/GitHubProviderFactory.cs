using VcsManager.Builder;
using VcsManager.Internal;

namespace VcsManager.GitHub;

/// <summary>
/// Creates <see cref="GitHubProvider"/> instances from flat configuration.
/// </summary>
public sealed class GitHubProviderFactory : IVcsProviderFactory
{
    /// <inheritdoc/>
    public string ProviderName => VcsProviderNames.GitHub;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The configuration is missing required settings.</exception>
    public IVcsProvider Create(IReadOnlyDictionary<string, string> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new GitHubProvider(MapConfiguration(configuration));
    }

    internal static GitHubOptions MapConfiguration(IReadOnlyDictionary<string, string> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new GitHubOptions();

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

        if (ProviderConfiguration.TryGetValue(configuration, "request.timeout", out var requestTimeout))
        {
            options.RequestTimeout = ProviderConfiguration.ParseTimeSpan(requestTimeout, "request.timeout");
        }

        return options;
    }

}
