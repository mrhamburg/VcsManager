using VcsManager.Builder;

namespace VcsManager.AzureDevOps;

/// <summary>
/// Extension methods that register the Azure DevOps provider.
/// </summary>
public static class VcsProviderBuilderExtensions
{
    /// <summary>
    /// Registers the Azure DevOps provider and optionally configures it through
    /// the fluent API. When <paramref name="configure"/> is <see langword="null"/>,
    /// configuration can come from <see cref="VcsProviderBuilder.FromEnvironment"/>,
    /// <see cref="VcsProviderBuilder.FromYaml(string)"/>, or
    /// <see cref="VcsProviderBuilder.UseProvider"/>.
    /// </summary>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="configure">An optional action that configures Azure DevOps options.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static VcsProviderBuilder WithAzureDevOps(
        this VcsProviderBuilder builder,
        Action<AzureDevOpsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterProvider(new AzureDevOpsProviderFactory());

        if (configure is null)
        {
            return builder;
        }

        var options = new AzureDevOpsOptions();
        configure(options);

        builder.UseProvider("azuredevops", settings =>
        {
            if (!string.IsNullOrWhiteSpace(options.OrganizationUrl))
            {
                settings["organization.url"] = options.OrganizationUrl;
            }

            if (!string.IsNullOrWhiteSpace(options.PersonalAccessToken))
            {
                settings["personal.access.token"] = options.PersonalAccessToken;
            }

            if (!string.IsNullOrWhiteSpace(options.DefaultProject))
            {
                settings["default.project"] = options.DefaultProject;
            }

            if (!string.IsNullOrWhiteSpace(options.ApiVersion))
            {
                settings["api.version"] = options.ApiVersion;
            }
        });

        return builder;
    }
}
