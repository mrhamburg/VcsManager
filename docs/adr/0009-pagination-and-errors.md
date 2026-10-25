# ADR-0009: Pagination and error mapping

- Status: Accepted
- Date: 2026-10-25

## Context

The providers paginate differently. GitHub uses `page`/`per_page` and a `Link`
header, GitLab uses `page`/`per_page` with total-count headers, and Azure DevOps
uses `$top`/`$skip` or continuation tokens. The providers also return errors as
different exception and HTTP types. Callers need one paging shape and one error
contract.

## Decision

### Pagination

- `PaginationOptions.Page` defaults to `1` and must be at least `1`.
- `PaginationOptions.PageSize` defaults to `30` and must be between `1` and
  `100`. All three providers support a page size of 100.
- `PagedResult<T>` carries `Items`, `Page`, `PageSize`, `TotalCount`, and
  `HasNextPage`.
- `TotalCount` is `null` when the provider does not report a total. It is never
  guessed.
- `HasNextPage` comes from the provider's continuation information, not from
  comparing `Items.Count` with `PageSize`.
- A request fetches exactly one page. The library does not fetch all pages in
  the background.

| Provider | Request parameters | Total count | Next page source |
| --- | --- | --- | --- |
| GitHub | `page`, `per_page` | Not reported; `TotalCount = null` | `Link` header with `rel="next"` |
| GitLab | `page`, `per_page` | `X-Total` header | `X-Next-Page` header |
| Azure DevOps | `$top`, `$skip`, or continuation token | Not reported; `TotalCount = null` | Continuation token in the response |

### Errors

Provider failures map to the core exception contract defined in VCS-004. The
categories and mapping rules are:

| Condition | Core category | Provider signals |
| --- | --- | --- |
| Missing or invalid token | Authentication | HTTP 401; `VssUnauthorizedException`; Octokit `AuthorizationException` |
| Insufficient permission | Authorization | HTTP 403; Azure DevOps `VssServiceException` for access denial |
| Missing repository, branch, or pull request | Not found | HTTP 404; Octokit `NotFoundException`; `VssResourceNotFoundException` |
| Invalid or blocked request | Validation or conflict | HTTP 409/422; provider validation payloads; blocked merge or completion |
| Method or provider capability unavailable | Unsupported operation | HTTP 405; documented provider limitation |
| Rate limit | Rate limit | HTTP 429, or GitHub 403 with exhausted rate-limit headers; include the retry delay when provided |
| Network failure or 5xx | Provider unavailable (transient) | `HttpRequestException`, timeouts, and HTTP 5xx responses |

Rules:

- Error messages include the provider name, operation, and a safe resource
  identifier. They never include tokens, authorization headers, or request
  bodies that can contain secrets.
- Provider-specific exceptions do not escape the provider package.
- A provider does not retry a non-idempotent operation implicitly. Retry policy
  is the caller's decision; the only built-in retry is for transport-safe reads
  if a provider client already performs it.
- Rate-limit delays are surfaced, not swallowed.

## Consequences

- Callers can page and handle failures without provider-specific code.
- `TotalCount` is nullable and consumers must handle `null`.
- Each provider test suite covers page metadata, last-page behavior, and each
  error category that the provider can produce.

## References

- GitHub pagination: [https://docs.github.com/en/rest/using-the-rest-api/using-pagination-in-the-rest-api](https://docs.github.com/en/rest/using-the-rest-api/using-pagination-in-the-rest-api)
- GitLab pagination: [https://docs.gitlab.com/api/rest/#pagination](https://docs.gitlab.com/api/rest/#pagination)
- Azure DevOps REST API 7.1: [https://learn.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-7.1](https://learn.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-7.1)
