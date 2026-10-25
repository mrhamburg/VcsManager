namespace VcsManager.AzureDevOps;

/// <summary>
/// Configures the Azure DevOps provider.
/// </summary>
public sealed class AzureDevOpsOptions
{
    /// <summary>
    /// The default Azure DevOps REST API version.
    /// </summary>
    public const string DefaultApiVersion = "7.1";

    /// <summary>
    /// Gets or sets the organization URL, for example
    /// <c>https://dev.azure.com/my-organization</c> or
    /// <c>https://my-organization.visualstudio.com</c>.
    /// </summary>
    public string? OrganizationUrl { get; set; }

    /// <summary>
    /// Gets or sets the personal access token used to authenticate.
    /// </summary>
    public string? PersonalAccessToken { get; set; }

    /// <summary>
    /// Gets or sets the team project used when a request does not specify one.
    /// </summary>
    public string? DefaultProject { get; set; }

    /// <summary>
    /// Gets or sets the Azure DevOps REST API version used for fallback calls.
    /// Defaults to <see cref="DefaultApiVersion"/>.
    /// </summary>
    public string ApiVersion { get; set; } = DefaultApiVersion;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(OrganizationUrl))
        {
            throw new ArgumentException(
                "An Azure DevOps organization URL is required. Set AzureDevOpsOptions.OrganizationUrl or the 'organization.url' configuration setting.",
                nameof(OrganizationUrl));
        }

        if (!Uri.TryCreate(OrganizationUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "The Azure DevOps organization URL must be an absolute HTTP or HTTPS URL.",
                nameof(OrganizationUrl));
        }

        if (string.IsNullOrWhiteSpace(PersonalAccessToken))
        {
            throw new ArgumentException(
                "An Azure DevOps personal access token is required. Set AzureDevOpsOptions.PersonalAccessToken or the 'personal.access.token' configuration setting.",
                nameof(PersonalAccessToken));
        }

        if (string.IsNullOrWhiteSpace(ApiVersion))
        {
            throw new ArgumentException(
                "An Azure DevOps API version is required.",
                nameof(ApiVersion));
        }

        _ = GetOrganizationName();
    }

    internal Uri CreateOrganizationUri()
    {
        return new Uri(OrganizationUrl!.Trim().TrimEnd('/') + "/", UriKind.Absolute);
    }

    internal string GetOrganizationName()
    {
        var uri = CreateOrganizationUri();
        const string visualStudioSuffix = ".visualstudio.com";

        if (uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                throw new ArgumentException(
                    "The Azure DevOps organization URL must include the organization name, for example https://dev.azure.com/my-organization.",
                    nameof(OrganizationUrl));
            }

            return segments[0];
        }

        if (uri.Host.EndsWith(visualStudioSuffix, StringComparison.OrdinalIgnoreCase))
        {
            var name = uri.Host[..^visualStudioSuffix.Length];
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "The Azure DevOps organization URL must include the organization name.",
                    nameof(OrganizationUrl));
            }

            return name;
        }

        var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return pathSegments.Length > 0 ? pathSegments[^1] : uri.Host;
    }
}
