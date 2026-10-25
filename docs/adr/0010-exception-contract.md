# ADR-0010: Exception contract

- Status: Accepted
- Date: 2026-10-25

## Context

GitHub, GitLab, and Azure DevOps report failures with different HTTP status
codes, SDK exceptions, and error bodies. Callers need one exception family so
provider switching does not change error handling. ADR-0009 defines the error
categories and pagination behavior; this ADR defines the concrete public
exception types and the provider status mapping.

## Decision

Every provider failure derives from `VcsProviderException`.

| Exception | Used when | Context properties |
| --- | --- | --- |
| `VcsProviderException` | Base type for all provider failures | None |
| `VcsNotFoundException` | A resource does not exist and no more specific type applies | `Resource` |
| `RepositoryNotFoundException` | A repository does not exist | `Owner`, `Project`, `Name` |
| `PullRequestNotFoundException` | A pull request does not exist | `Repository`, `Number` |
| `VcsAuthenticationException` | Credentials are missing or invalid | None |
| `VcsAuthorizationException` | Credentials are valid but lack permission | None |
| `VcsRateLimitException` | A provider rate limit is exceeded | `RetryAfter` (nullable) |
| `VcsConflictException` | Merge conflict, changed source commit, or invalid state transition | None |
| `VcsUnsupportedOperationException` | The provider or current project settings cannot perform the operation | `Provider`, `Operation` |

### HTTP status mapping

| Provider status | Public exception | Notes |
| --- | --- | --- |
| 400 | `VcsProviderException` | Malformed or invalid request. v1 has no separate validation exception. |
| 401 | `VcsAuthenticationException` | Missing, expired, or invalid token. |
| 403 | `VcsAuthorizationException` | Valid token without permission. If rate-limit headers are present, use `VcsRateLimitException` instead. |
| 404 | `VcsNotFoundException` | Use `RepositoryNotFoundException` or `PullRequestNotFoundException` when the resource type is known. |
| 405 | `VcsUnsupportedOperationException` | The provider cannot perform the operation. |
| 409 | `VcsConflictException` | Merge conflict, stale source commit, or invalid state transition. |
| 422 | `VcsConflictException` or `VcsProviderException` | Use `VcsConflictException` for state conflicts; otherwise use `VcsProviderException`. |
| 429 | `VcsRateLimitException` | Set `RetryAfter` from the provided delay or reset time, otherwise `null`. |
| 5xx or network failure | `VcsProviderException` | Wrap the original exception. Treat the failure as transient. |

Provider-specific signals map to the same types:

- **GitHub.** Octokit `AuthorizationException`, `NotFoundException`,
  `RateLimitExceededException`, and `ApiException` are translated by the
  provider.
- **GitLab.** REST response status codes and `HttpRequestException` are
  translated by the provider.
- **Azure DevOps.** `VssUnauthorizedException`, `VssResourceNotFoundException`,
  and `VssServiceException` are translated by the provider.

Rules:

- Public methods never throw raw `HttpRequestException`, Octokit exception
  types, or Azure DevOps SDK exception types. They become the inner exception
  of the mapped VcsManager exception.
- Authentication and authorization stay separate because the caller action
  differs: supply a new token versus request more permission.
- `VcsRateLimitException.RetryAfter` is `null` when the provider gives no delay
  or reset time.
- `VcsConflictException` covers merge conflicts and merge attempts against a
  source commit that changed after the caller read it.
- `VcsUnsupportedOperationException` covers merge methods that the provider
  cannot execute for the current project settings, and other unsupported
  operations.
- Messages include the provider, operation, and safe resource identifiers.
  They never include tokens, authorization headers, or full request bodies.

## Consequences

- Callers catch one base type for generic handling and specific types for
  targeted recovery.
- Provider packages own the translation from native errors. The core project
  has no provider dependency.
- Each provider test suite must cover status mapping and inner-exception
  preservation.
- A new error category requires a new exception type and an update to this ADR.

## References

- GitHub REST API status codes: [https://docs.github.com/en/rest/using-the-rest-api/troubleshooting-the-rest-api](https://docs.github.com/en/rest/using-the-rest-api/troubleshooting-the-rest-api)
- GitLab REST API status codes: [https://docs.gitlab.com/api/rest/#http-status-codes](https://docs.gitlab.com/api/rest/#http-status-codes)
- Azure DevOps REST API 7.1: [https://learn.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-7.1](https://learn.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-7.1)
