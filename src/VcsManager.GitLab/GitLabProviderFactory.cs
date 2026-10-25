using VcsManager.Builder;

namespace VcsManager.GitLab;

/// <summary>
/// Creates <see cref="GitLabProvider"/> instances from flat configuration.
/// </summary>
public sealed class GitLabProviderFactory : IVcsProviderFactory
{
    /// <inheritdoc/>
    public string ProviderName => "gitlab";

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

        if (TryGetValue(configuration, "token", out var token))
        {
            options.Token = token;
        }

        if (TryGetValue(configuration, "base.url", out var baseUrl))
        {
            options.BaseUrl = baseUrl;
        }

        if (TryGetValue(configuration, "default.namespace", out var defaultNamespace))
        {
            options.DefaultNamespace = defaultNamespace;
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
