using System.Net;
using System.Text.Json;

namespace VcsManager.GitLab.Internal;

internal static class GitLabExceptionMapper
{
    private const string ProviderName = "gitlab";

    public static Exception MapRepository(
        Exception exception,
        string operation,
        RepositoryRef repository)
    {
        return Map(
            exception,
            operation,
            $"repository '{FormatRepositoryName(repository)}'",
            inner => new RepositoryNotFoundException(
                repository.Owner,
                repository.Project,
                repository.Name,
                inner));
    }

    public static Exception MapPullRequest(
        Exception exception,
        string operation,
        RepositoryRef repository,
        int iid)
    {
        return Map(
            exception,
            operation,
            $"merge request !{iid} in repository '{FormatRepositoryName(repository)}'",
            inner => new PullRequestNotFoundException(repository, iid, inner));
    }

    public static Exception MapOperation(Exception exception, string operation)
    {
        return Map(
            exception,
            operation,
            "the GitLab API",
            inner => new VcsNotFoundException(
                "resource",
                FormatMessage(operation, "the GitLab API"),
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
            GitLabApiException api => MapApiException(api, operation, message, notFound),
            HttpRequestException http => new VcsProviderException(
                $"{message}: {http.Message}",
                http),
            TimeoutException timeout => new VcsProviderException(
                $"{message}: {timeout.Message}",
                timeout),
            JsonException json => new VcsProviderException(
                $"{message}: the response was not valid JSON.",
                json),
            _ => new VcsProviderException($"{message}: {exception.Message}", exception),
        };
    }

    private static Exception MapApiException(
        GitLabApiException exception,
        string operation,
        string message,
        Func<Exception, Exception> notFound)
    {
        var detail = SafeDetail(exception.ResponseBody);

        return exception.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new VcsAuthenticationException(
                $"{message}{detail}",
                exception),
            HttpStatusCode.Forbidden => exception.RetryAfter is null
                ? new VcsAuthorizationException($"{message}{detail}", exception)
                : new VcsRateLimitException($"{message}{detail}", exception.RetryAfter, exception),
            HttpStatusCode.NotFound => notFound(exception),
            HttpStatusCode.MethodNotAllowed => new VcsUnsupportedOperationException(
                ProviderName,
                operation,
                exception),
            HttpStatusCode.Conflict => new VcsConflictException(
                $"{message}{detail}",
                exception),
            HttpStatusCode.UnprocessableEntity => new VcsConflictException(
                $"{message}{detail}",
                exception),
            HttpStatusCode.TooManyRequests => new VcsRateLimitException(
                $"{message}{detail}",
                exception.RetryAfter,
                exception),
            HttpStatusCode.BadRequest when IsConflict(exception.ResponseBody) => new VcsConflictException(
                $"{message}{detail}",
                exception),
            HttpStatusCode.BadRequest => new VcsProviderException(
                $"{message}{detail}",
                exception),
            >= HttpStatusCode.InternalServerError => new VcsProviderException(
                $"{message}{detail}",
                exception),
            _ => new VcsProviderException(
                $"{message}{detail}",
                exception),
        };
    }

    private static bool IsConflict(string responseBody)
    {
        return responseBody.Contains("already been taken", StringComparison.OrdinalIgnoreCase) ||
            responseBody.Contains("already exists", StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeDetail(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return string.Empty;
        }

        var detail = responseBody.Trim().ReplaceLineEndings(" ");
        const int maxLength = 300;
        if (detail.Length > maxLength)
        {
            detail = detail[..maxLength];
        }

        return $": {detail}";
    }

    private static string FormatMessage(string operation, string target)
    {
        return $"GitLab {operation} failed for {target}.";
    }

    private static string FormatRepositoryName(RepositoryRef repository)
    {
        var segments = new[] { repository.Owner, repository.Project, repository.Name }
            .Where(segment => !string.IsNullOrWhiteSpace(segment));

        return string.Join("/", segments);
    }
}
