namespace VcsManager.GitHub;

/// <summary>
/// Configures the GitHub provider.
/// </summary>
public sealed class GitHubOptions
{
    /// <summary>
    /// The default GitHub REST API base URL.
    /// </summary>
    public const string DefaultBaseUrl = "https://api.github.com";

    /// <summary>
    /// Gets or sets the personal access token used to authenticate.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the GitHub REST API base URL. Defaults to
    /// <see cref="DefaultBaseUrl"/>. For GitHub Enterprise Server use the
    /// server API URL, for example <c>https://github.example.com/api/v3</c>.
    /// </summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>
    /// Gets or sets the default owner, organization, or user used when a
    /// request does not specify one.
    /// </summary>
    public string? DefaultOwner { get; set; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            throw new ArgumentException(
                "A GitHub token is required. Set GitHubOptions.Token or the 'token' configuration setting.",
                nameof(Token));
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new ArgumentException(
                "A GitHub base URL is required. Set GitHubOptions.BaseUrl or the 'base.url' configuration setting.",
                nameof(BaseUrl));
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "The GitHub base URL must be an absolute HTTP or HTTPS URL.",
                nameof(BaseUrl));
        }
    }
}
