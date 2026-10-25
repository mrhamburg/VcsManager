using System.Globalization;
using VcsManager.Builder;

namespace VcsManager.GitHub;

/// <summary>
/// Extension methods that register the GitHub provider.
/// </summary>
public static class VcsProviderBuilderExtensions
{
    /// <summary>
    /// Registers the GitHub provider and optionally configures it through the
    /// fluent API. When <paramref name="configure"/> is <see langword="null"/>,
    /// configuration can come from <see cref="VcsProviderBuilder.FromEnvironment"/>,
    /// <see cref="VcsProviderBuilder.FromYaml(string)"/>, or
    /// <see cref="VcsProviderBuilder.UseProvider"/>.
    /// </summary>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="configure">An optional action that configures GitHub options.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static VcsProviderBuilder WithGitHub(
        this VcsProviderBuilder builder,
        Action<GitHubOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterProvider(new GitHubProviderFactory());

        if (configure is null)
        {
            return builder;
        }

        var options = new GitHubOptions();
        configure(options);

        builder.UseProvider("github", settings =>
        {
            if (!string.IsNullOrWhiteSpace(options.Token))
            {
                settings["token"] = options.Token;
            }

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                settings["base.url"] = options.BaseUrl;
            }

            if (!string.IsNullOrWhiteSpace(options.DefaultOwner))
            {
                settings["default.owner"] = options.DefaultOwner;
            }

            settings["request.timeout"] = options.RequestTimeout.ToString("c", CultureInfo.InvariantCulture);
        });

        return builder;
    }
}
