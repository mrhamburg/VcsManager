namespace VcsManager.GitLab;

/// <summary>
/// Configures the GitLab provider.
/// </summary>
public sealed class GitLabOptions
{
    /// <summary>
    /// The default GitLab base URL.
    /// </summary>
    public const string DefaultBaseUrl = "https://gitlab.com";

    /// <summary>
    /// Gets or sets the personal access token used to authenticate.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the GitLab base URL. Defaults to
    /// <see cref="DefaultBaseUrl"/>. For self-hosted GitLab use the server
    /// URL, for example <c>https://gitlab.example.com</c>.
    /// </summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>
    /// Gets or sets the default namespace used when a request does not specify
    /// an owner.
    /// </summary>
    public string? DefaultNamespace { get; set; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            throw new ArgumentException(
                "A GitLab token is required. Set GitLabOptions.Token or the 'token' configuration setting.",
                nameof(Token));
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new ArgumentException(
                "A GitLab base URL is required. Set GitLabOptions.BaseUrl or the 'base.url' configuration setting.",
                nameof(BaseUrl));
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "The GitLab base URL must be an absolute HTTP or HTTPS URL.",
                nameof(BaseUrl));
        }
    }

    internal Uri CreateApiBaseAddress()
    {
        var baseUrl = BaseUrl.TrimEnd('/');
        if (!baseUrl.EndsWith("/api/v4", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl += "/api/v4";
        }

        return new Uri(baseUrl + "/", UriKind.Absolute);
    }
}
