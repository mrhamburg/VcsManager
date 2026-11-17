using System.Net;
using System.Text.Json;
using Microsoft.TeamFoundation;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Exceptions;

namespace VcsManager.AzureDevOps.Internal;

internal static class AzureDevOpsExceptionMapper
{
    public static Exception MapRepository(
        Exception exception,
        string operation,
        string organization,
        string? project,
        string name)
    {
        return Map(
            exception,
            operation,
            $"repository '{FormatRepositoryName(organization, project, name)}'",
            inner => new RepositoryNotFoundException(organization, project, name, inner),
            conflictOnBadRequest: false);
    }

    public static Exception MapPullRequest(
        Exception exception,
        string operation,
        RepositoryRef repository,
        int number)
    {
        return Map(
            exception,
            operation,
            $"pull request #{number} in repository '{FormatRepositoryName(repository)}'",
            inner => new PullRequestNotFoundException(repository, number, inner),
            conflictOnBadRequest: false);
    }

    public static Exception MapMergePullRequest(
        Exception exception,
        string operation,
        RepositoryRef repository,
        int number)
    {
        return Map(
            exception,
            operation,
            $"pull request #{number} in repository '{FormatRepositoryName(repository)}'",
            inner => new PullRequestNotFoundException(repository, number, inner),
            conflictOnBadRequest: true);
    }

    public static Exception MapOperation(Exception exception, string operation)
    {
        return Map(
            exception,
            operation,
            "the Azure DevOps API",
            inner => new VcsNotFoundException(
                "resource",
                $"Azure DevOps {operation} failed for the Azure DevOps API.",
                inner),
            conflictOnBadRequest: false);
    }

    private static Exception Map(
        Exception exception,
        string operation,
        string target,
        Func<Exception, Exception> notFound,
        bool conflictOnBadRequest)
    {
        var message = $"Azure DevOps {operation} failed for {target}.";

        return exception switch
        {
            AzureDevOpsApiException api => MapStatus(
                api.StatusCode,
                message,
                notFound,
                conflictOnBadRequest,
                api),
            VssServiceResponseException response => MapStatus(
                response.HttpStatusCode,
                message,
                notFound,
                conflictOnBadRequest,
                response),
            TooManyRequestsException throttled => new VcsRateLimitException(
                $"{message} {throttled.Message}".TrimEnd(),
                GetRetryAfter(throttled),
                throttled),
            VssUnauthorizedException vssUnauthorized => new VcsAuthenticationException(
                $"{message} {vssUnauthorized.Message}".TrimEnd(),
                vssUnauthorized),
            VssResourceNotFoundException resourceNotFound => notFound(resourceNotFound),
            VssAuthenticationException authentication => new VcsAuthenticationException(
                $"{message} {authentication.Message}".TrimEnd(),
                authentication),
            TeamFoundationServerUnauthorizedException unauthorized => new VcsAuthenticationException(
                $"{message} {unauthorized.Message}".TrimEnd(),
                unauthorized),
            HttpRequestException http => new VcsProviderException(
                $"{message} {http.Message}".TrimEnd(),
                http,
                isTransient: true),
            TimeoutException timeout => new VcsProviderException(
                $"{message} {timeout.Message}".TrimEnd(),
                timeout,
                isTransient: true),
            OperationCanceledException canceled => new VcsProviderException(
                $"{message} The request timed out.",
                canceled,
                isTransient: true),
            JsonException json => new VcsProviderException(
                $"{message} The response was not valid JSON.",
                json),
            _ => new VcsProviderException($"{message} {exception.Message}".TrimEnd(), exception),
        };
    }

    private static Exception MapStatus(
        HttpStatusCode statusCode,
        string message,
        Func<Exception, Exception> notFound,
        bool conflictOnBadRequest,
        Exception exception)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => new VcsAuthenticationException(message, exception),
            HttpStatusCode.Forbidden => new VcsAuthorizationException(message, exception),
            HttpStatusCode.NotFound => notFound(exception),
            HttpStatusCode.Conflict => new VcsConflictException(message, exception),
            HttpStatusCode.BadRequest when conflictOnBadRequest => new VcsConflictException(
                message,
                exception),
            HttpStatusCode.TooManyRequests => new VcsRateLimitException(
                message,
                retryAfter: (exception as AzureDevOpsApiException)?.RetryAfter,
                exception),
            >= HttpStatusCode.InternalServerError => new VcsProviderException(
                message,
                exception,
                isTransient: true),
            _ => new VcsProviderException(message, exception),
        };
    }

    private static TimeSpan? GetRetryAfter(TooManyRequestsException exception)
    {
        var delay = exception.RetryAfterDateTime - DateTime.UtcNow;
        return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
    }

    private static string FormatRepositoryName(RepositoryRef repository)
    {
        return FormatRepositoryName(repository.Owner, repository.Project, repository.Name);
    }

    private static string FormatRepositoryName(string? organization, string? project, string name)
    {
        var segments = new[] { organization, project, name }
            .Where(segment => !string.IsNullOrWhiteSpace(segment));

        return string.Join("/", segments);
    }
}
