using System.Net;
using System.Text.Json;
using VcsManager.Internal;

namespace VcsManager.Gitea.Internal;

internal static class GiteaExceptionMapper
{
    public static Exception MapRepository(
        Exception exception,
        string operation,
        string? owner,
        string name)
    {
        return Map(
            exception,
            operation,
            $"repository '{FormatRepositoryName(owner, name)}'",
            inner => new RepositoryNotFoundException(owner, project: null, name, inner));
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
            inner => new PullRequestNotFoundException(repository, number, inner));
    }

    public static Exception MapOperation(Exception exception, string operation)
    {
        return Map(
            exception,
            operation,
            "the Gitea API",
            inner => new VcsNotFoundException(
                "resource",
                FormatMessage(operation, "the Gitea API"),
                inner));
    }

    private static Exception Map(
        Exception exception,
        string operation,
        string target,
        Func<Exception, Exception> notFound)
    {
        if (exception is VcsProviderException)
        {
            return exception;
        }

        var message = FormatMessage(operation, target);

        return exception switch
        {
            GiteaApiException api => MapApiException(api, operation, message, notFound),
            HttpRequestException http => new VcsProviderException(
                $"{message}: {http.Message}",
                http,
                isTransient: true),
            TimeoutException timeout => new VcsProviderException(
                $"{message}: {timeout.Message}",
                timeout,
                isTransient: true),
            OperationCanceledException canceled => new VcsProviderException(
                $"{message}: the request timed out.",
                canceled,
                isTransient: true),
            JsonException json => new VcsProviderException(
                $"{message}: the response was not valid JSON.",
                json),
            _ => new VcsProviderException($"{message}: {exception.Message}", exception),
        };
    }

    private static Exception MapApiException(
        GiteaApiException exception,
        string operation,
        string message,
        Func<Exception, Exception> notFound)
    {
        return exception.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new VcsAuthenticationException(message, exception),
            HttpStatusCode.Forbidden => exception.RetryAfter is null
                ? new VcsAuthorizationException(message, exception)
                : new VcsRateLimitException(message, exception.RetryAfter, exception),
            HttpStatusCode.NotFound => notFound(exception),
            HttpStatusCode.MethodNotAllowed => new VcsUnsupportedOperationException(
                VcsProviderNames.Gitea,
                operation,
                exception),
            HttpStatusCode.Conflict => new VcsConflictException(message, exception),
            HttpStatusCode.UnprocessableEntity => new VcsConflictException(message, exception),
            HttpStatusCode.Locked => new VcsConflictException(message, exception),
            HttpStatusCode.TooManyRequests => new VcsRateLimitException(
                message,
                exception.RetryAfter,
                exception),
            HttpStatusCode.BadRequest => new VcsProviderException(message, exception),
            >= HttpStatusCode.InternalServerError => new VcsProviderException(
                message,
                exception,
                isTransient: true),
            _ => new VcsProviderException(message, exception),
        };
    }

    private static string FormatMessage(string operation, string target)
    {
        return $"Gitea {operation} failed for {target}.";
    }

    private static string FormatRepositoryName(RepositoryRef repository)
    {
        return FormatRepositoryName(repository.Owner, repository.Name);
    }

    private static string FormatRepositoryName(string? owner, string name)
    {
        return string.IsNullOrWhiteSpace(owner) ? name : $"{owner}/{name}";
    }
}
