using VcsManager.Builder;

namespace VcsManager.Gitea;

/// <summary>
/// Extension methods that register the Gitea provider.
/// </summary>
public static class VcsProviderBuilderExtensions
{
    /// <summary>
    /// Registers the Gitea provider and optionally configures it through the
    /// fluent API. When <paramref name="configure"/> is <see langword="null"/>,
    /// configuration can come from <see cref="VcsProviderBuilder.FromEnvironment"/>,
    /// <see cref="VcsProviderBuilder.FromYaml(string)"/>, or
    /// <see cref="VcsProviderBuilder.UseProvider"/>.
    /// </summary>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="configure">An optional action that configures Gitea options.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static VcsProviderBuilder WithGitea(
        this VcsProviderBuilder builder,
        Action<GiteaOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterProvider(new GiteaProviderFactory());

        if (configure is null)
        {
            return builder;
        }

        var options = new GiteaOptions();
        configure(options);

        builder.UseProvider("gitea", settings =>
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
        });

        return builder;
    }
}
