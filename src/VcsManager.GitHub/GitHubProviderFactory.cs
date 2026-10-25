using System.Globalization;
using VcsManager.Builder;

namespace VcsManager.GitHub;

/// <summary>
/// Creates <see cref="GitHubProvider"/> instances from flat configuration.
/// </summary>
public sealed class GitHubProviderFactory : IVcsProviderFactory
{
    /// <inheritdoc/>
    public string ProviderName => "github";

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

        if (TryGetValue(configuration, "token", out var token))
        {
            options.Token = token;
        }

        if (TryGetValue(configuration, "base.url", out var baseUrl))
        {
            options.BaseUrl = baseUrl;
        }

        if (TryGetValue(configuration, "default.owner", out var defaultOwner))
        {
            options.DefaultOwner = defaultOwner;
        }

        if (TryGetValue(configuration, "request.timeout", out var requestTimeout))
        {
            if (!TimeSpan.TryParseExact(requestTimeout, "c", CultureInfo.InvariantCulture, out var timeout))
            {
                throw new ArgumentException(
                    "The 'request.timeout' configuration setting must be a time span such as '00:01:40'.",
                    nameof(configuration));
            }

            options.RequestTimeout = timeout;
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
