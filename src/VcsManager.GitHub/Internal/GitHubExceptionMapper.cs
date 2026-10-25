using System.Net;
using Octokit;

namespace VcsManager.GitHub.Internal;

internal static class GitHubExceptionMapper
{
    private const string ProviderName = "github";

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
            inner => new RepositoryNotFoundException(owner, null, name, inner));
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

    public static Exception MapBranch(
        Exception exception,
        string operation,
        string owner,
        string name,
        string branch)
    {
        return Map(
            exception,
            operation,
            $"branch '{branch}' in repository '{FormatRepositoryName(owner, name)}'",
            inner => new VcsNotFoundException(
                "branch",
                $"Branch '{branch}' was not found in repository '{FormatRepositoryName(owner, name)}'.",
                inner));
    }

    public static bool IsInternalVisibilityUnsupported(Exception exception)
    {
        if (exception is not ApiException apiException ||
            apiException.StatusCode != HttpStatusCode.UnprocessableEntity)
        {
            return false;
        }

        if (apiException.Message.Contains("internal", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (apiException.ApiError?.Message?.Contains("internal", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return apiException.ApiError?.Errors?.Any(detail =>
            detail.Message?.Contains("internal", StringComparison.OrdinalIgnoreCase) == true ||
            detail.Code?.Contains("internal", StringComparison.OrdinalIgnoreCase) == true) == true;
    }

    public static Exception MapOperation(Exception exception, string operation)
    {
        return Map(
            exception,
            operation,
            "the GitHub API",
            inner => new VcsNotFoundException(
                "resource",
                FormatMessage(operation, "the GitHub API"),
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
            RateLimitExceededException rateLimit => new VcsRateLimitException(
                $"{message}: {rateLimit.Message}",
                rateLimit.GetRetryAfterTimeSpan(),
                rateLimit),
            SecondaryRateLimitExceededException secondaryRateLimit => new VcsRateLimitException(
                $"{message}: {secondaryRateLimit.Message}",
                GetRetryAfter(secondaryRateLimit),
                secondaryRateLimit),
            AuthorizationException authorization => new VcsAuthenticationException(
                $"{message}: {authorization.Message}",
                authorization),
            NotFoundException notFoundException => notFound(notFoundException),
            ForbiddenException forbidden => new VcsAuthorizationException(
                $"{message}: {forbidden.Message}",
                forbidden),
            PullRequestNotMergeableException conflict => new VcsConflictException(
                $"{message}: {conflict.Message}",
                conflict),
            PullRequestMismatchException conflict => new VcsConflictException(
                $"{message}: {conflict.Message}",
                conflict),
            ApiValidationException validation => new VcsConflictException(
                $"{message}: {validation.Message}",
                validation),
            ApiException api => MapApiException(api, operation, message, notFound),
            HttpRequestException http => new VcsProviderException(
                $"{message}: {http.Message}",
                http,
                isTransient: true),
            TimeoutException timeout => new VcsProviderException(
                $"{message}: {timeout.Message}",
                timeout,
                isTransient: true),
            _ => new VcsProviderException($"{message}: {exception.Message}", exception),
        };
    }

    private static Exception MapApiException(
        ApiException exception,
        string operation,
        string message,
        Func<Exception, Exception> notFound)
    {
        return exception.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new VcsAuthenticationException(
                $"{message}: {exception.Message}",
                exception),
            HttpStatusCode.Forbidden => new VcsAuthorizationException(
                $"{message}: {exception.Message}",
                exception),
            HttpStatusCode.NotFound => notFound(exception),
            HttpStatusCode.MethodNotAllowed => new VcsUnsupportedOperationException(
                ProviderName,
                operation,
                exception),
            HttpStatusCode.Conflict => new VcsConflictException(
                $"{message}: {exception.Message}",
                exception),
            HttpStatusCode.UnprocessableEntity => new VcsConflictException(
                $"{message}: {exception.Message}",
                exception),
            HttpStatusCode.TooManyRequests => new VcsRateLimitException(
                $"{message}: {exception.Message}",
                GetRetryAfter(exception),
                exception),
            >= HttpStatusCode.InternalServerError => new VcsProviderException(
                $"{message}: {exception.Message}",
                exception,
                isTransient: true),
            _ => new VcsProviderException($"{message}: {exception.Message}", exception),
        };
    }

    private static TimeSpan? GetRetryAfter(ApiException exception)
    {
        var reset = exception.HttpResponse?.ApiInfo?.RateLimit?.Reset;
        if (reset is null)
        {
            return null;
        }

        var delay = reset.Value - DateTimeOffset.UtcNow;
        return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
    }

    private static string FormatMessage(string operation, string target)
    {
        return $"GitHub {operation} failed for {target}.";
    }

    private static string FormatRepositoryName(string? owner, string name)
    {
        return string.IsNullOrWhiteSpace(owner) ? name : $"{owner}/{name}";
    }

    private static string FormatRepositoryName(RepositoryRef repository)
    {
        return FormatRepositoryName(repository.Owner, repository.Name);
    }
}
