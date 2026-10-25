using System.Net;

namespace VcsManager.AzureDevOps.Internal;

internal sealed class AzureDevOpsApiException : Exception
{
    public AzureDevOpsApiException(HttpStatusCode statusCode, string responseBody)
        : base($"Azure DevOps returned HTTP {(int)statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }
}
