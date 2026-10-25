using System.Net;

namespace VcsManager.GitLab.Internal;

internal sealed class GitLabApiException : Exception
{
    public GitLabApiException(
        HttpStatusCode statusCode,
        string responseBody,
        TimeSpan? retryAfter)
        : base($"GitLab API request failed with status {(int)statusCode} ({statusCode}).")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        RetryAfter = retryAfter;
    }

    public HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }

    public TimeSpan? RetryAfter { get; }
}
