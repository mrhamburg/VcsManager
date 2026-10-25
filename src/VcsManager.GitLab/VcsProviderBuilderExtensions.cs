using VcsManager.Builder;

namespace VcsManager.GitLab;

/// <summary>
/// Extension methods that register the GitLab provider.
/// </summary>
public static class VcsProviderBuilderExtensions
{
    /// <summary>
    /// Registers the GitLab provider and optionally configures it through the
    /// fluent API. When <paramref name="configure"/> is <see langword="null"/>,
    /// configuration can come from <see cref="VcsProviderBuilder.FromEnvironment"/>,
    /// <see cref="VcsProviderBuilder.FromYaml(string)"/>, or
    /// <see cref="VcsProviderBuilder.UseProvider"/>.
    /// </summary>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="configure">An optional action that configures GitLab options.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static VcsProviderBuilder WithGitLab(
        this VcsProviderBuilder builder,
        Action<GitLabOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterProvider(new GitLabProviderFactory());

        if (configure is null)
        {
            return builder;
        }

        var options = new GitLabOptions();
        configure(options);

        builder.UseProvider("gitlab", settings =>
        {
            if (!string.IsNullOrWhiteSpace(options.Token))
            {
                settings["token"] = options.Token;
            }

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                settings["base.url"] = options.BaseUrl;
            }

            if (!string.IsNullOrWhiteSpace(options.DefaultNamespace))
            {
                settings["default.namespace"] = options.DefaultNamespace;
            }
        });

        return builder;
    }
}
