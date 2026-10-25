namespace VcsManager.Gitea;

/// <summary>
/// Configures the Gitea provider.
/// </summary>
public sealed class GiteaOptions
{
    /// <summary>
    /// Gets or sets the Gitea base URL, for example
    /// <c>https://gitea.example.com</c>. The URL must include the scheme.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the access token used to authenticate.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the default owner used when a request does not specify one.
    /// </summary>
    public string? DefaultOwner { get; set; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new ArgumentException(
                "A Gitea base URL is required. Set GiteaOptions.BaseUrl or the 'base.url' configuration setting.",
                nameof(BaseUrl));
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "The Gitea base URL must be an absolute HTTP or HTTPS URL.",
                nameof(BaseUrl));
        }

        if (string.IsNullOrWhiteSpace(Token))
        {
            throw new ArgumentException(
                "A Gitea token is required. Set GiteaOptions.Token or the 'token' configuration setting.",
                nameof(Token));
        }
    }

    internal Uri CreateApiBaseAddress()
    {
        var baseUrl = BaseUrl!.Trim().TrimEnd('/');
        if (!baseUrl.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl += "/api/v1";
        }

        return new Uri(baseUrl + "/", UriKind.Absolute);
    }
}
