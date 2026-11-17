using VcsManager.Builder;
using VcsManager.Internal;

namespace VcsManager.GitLab;

/// <summary>
/// Creates <see cref="GitLabProvider"/> instances from flat configuration.
/// </summary>
public sealed class GitLabProviderFactory : IVcsProviderFactory
{
    /// <inheritdoc/>
    public string ProviderName => VcsProviderNames.GitLab;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The configuration is missing required settings.</exception>
    public IVcsProvider Create(IReadOnlyDictionary<string, string> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new GitLabProvider(MapConfiguration(configuration));
    }

    internal static GitLabOptions MapConfiguration(IReadOnlyDictionary<string, string> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new GitLabOptions();

        if (ProviderConfiguration.TryGetValue(configuration, "token", out var token))
        {
            options.Token = token;
        }

        if (ProviderConfiguration.TryGetValue(configuration, "base.url", out var baseUrl))
        {
            options.BaseUrl = baseUrl;
        }

        if (ProviderConfiguration.TryGetValue(configuration, "default.namespace", out var defaultNamespace))
        {
            options.DefaultNamespace = defaultNamespace;
        }

        return options;
    }

}
