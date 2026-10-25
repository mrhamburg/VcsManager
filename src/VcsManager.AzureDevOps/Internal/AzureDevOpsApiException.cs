using System.Net;

namespace VcsManager.AzureDevOps.Internal;

internal sealed class AzureDevOpsApiException : Exception
{
    public AzureDevOpsApiException(
        HttpStatusCode statusCode,
        string responseBody,
        TimeSpan? retryAfter = null)
        : base($"Azure DevOps returned HTTP {(int)statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        RetryAfter = retryAfter;
    }

    public HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }

    public TimeSpan? RetryAfter { get; }
}
