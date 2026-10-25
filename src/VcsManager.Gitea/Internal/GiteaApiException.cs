using System.Net;

namespace VcsManager.Gitea.Internal;

internal sealed class GiteaApiException : Exception
{
    public GiteaApiException(
        HttpStatusCode statusCode,
        string responseBody,
        TimeSpan? retryAfter)
        : base($"Gitea returned HTTP {(int)statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        RetryAfter = retryAfter;
    }

    public HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }

    public TimeSpan? RetryAfter { get; }
}
