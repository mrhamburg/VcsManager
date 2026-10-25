---
id: pagination-and-errors
title: Pagination and Errors
sidebar_label: Pagination and Errors
sidebar_position: 3
---

# Pagination and Errors

Listing calls and error handling use the same shape across every provider.

## Pagination

`PaginationOptions` controls one page request:

| Property | Default | Valid range |
| --- | --- | --- |
| `Page` | `1` | `1` or greater |
| `PageSize` | `30` | `1` to `100` |

A value outside the range throws `ArgumentOutOfRangeException` before the
provider is called.

`PagedResult<T>` carries one page:

| Property | Description |
| --- | --- |
| `Items` | The items on this page. |
| `Page` | The page number. |
| `PageSize` | The effective page size. |
| `TotalCount` | The total number of items, or `null` when the provider does not report one. |
| `HasNextPage` | Whether another page is available. |

The provider fetches exactly one page. VcsManager never fetches all pages in
the background.

### Provider pagination

| Provider | Request | `TotalCount` | `HasNextPage` | Notes |
| --- | --- | --- | --- | --- |
| GitHub | `page`, `per_page` | `null` | `Link` header with `rel="next"` | GitHub does not report a total for these endpoints. |
| GitLab | `page`, `per_page` | `X-Total` header | `X-Next-Page` header | Archived filter is passed to GitLab. |
| Azure DevOps | `$top`, `$skip`, or continuation | `null` | Computed by requesting one item more than `PageSize` | Organization-level listing is limited to one organization. |
| Gitea | `page`, `limit` | `X-Total-Count` when supplied | `Link` header with `rel="next"` | `limit` is capped at 50. |

### Iterate over pages

```csharp
var pageNumber = PaginationOptions.DefaultPage;

while (true)
{
    var page = await provider.ListRepositoriesAsync(
        new RepositoryQuery { Owner = "my-org", IncludeArchived = false },
        new PaginationOptions { Page = pageNumber, PageSize = 50 });

    foreach (var repository in page.Items)
    {
        Console.WriteLine(repository.FullName);
    }

    if (!page.HasNextPage)
    {
        break;
    }

    pageNumber++;
}
```

Do not compare `Items.Count` with `PageSize` to decide whether another page
exists. Providers can return a short page while more results remain. Use
`HasNextPage`.

## Errors

Every provider failure derives from `VcsProviderException`:

| Exception | Meaning | Useful properties |
| --- | --- | --- |
| `VcsProviderException` | Base type for provider failures. | `IsTransient` |
| `VcsNotFoundException` | A resource does not exist. | `Resource` |
| `RepositoryNotFoundException` | A repository does not exist. | `Owner`, `Project`, `Name` |
| `PullRequestNotFoundException` | A pull request does not exist. | `Repository`, `Number` |
| `VcsAuthenticationException` | Credentials are missing or invalid. | None |
| `VcsAuthorizationException` | Credentials are valid but lack permission. | None |
| `VcsRateLimitException` | A provider rate limit is exceeded. | `RetryAfter` |
| `VcsConflictException` | Merge conflict, changed source commit, or invalid state transition. | None |
| `VcsUnsupportedOperationException` | The provider or current settings cannot perform the operation. | `Provider`, `Operation` |

### General HTTP mapping

| Provider response | Core exception |
| --- | --- |
| 401 Unauthorized | `VcsAuthenticationException` |
| 403 Forbidden | `VcsAuthorizationException`, unless the response is a rate limit |
| 404 Not Found | `RepositoryNotFoundException`, `PullRequestNotFoundException`, or `VcsNotFoundException` |
| 405 Method Not Allowed | `VcsUnsupportedOperationException` for GitHub and GitLab; `VcsConflictException` for Gitea; otherwise `VcsProviderException` |
| 409 Conflict | `VcsConflictException` |
| 422 Unprocessable Entity | `VcsConflictException` or `VcsProviderException` |
| 429 Too Many Requests | `VcsRateLimitException` |
| 5xx or network failure | `VcsProviderException` with `IsTransient = true` |

### Provider nuances

- **GitHub.** Octokit rate-limit exceptions become `VcsRateLimitException` with
  `RetryAfter` from the reset time. Octokit authorization failures become
  `VcsAuthenticationException`. A 403 without rate-limit headers becomes
  `VcsAuthorizationException`.
- **GitLab.** A 403 with a `Retry-After` header becomes
  `VcsRateLimitException`; otherwise it becomes `VcsAuthorizationException`.
  HTTP 400 with a duplicate or already-taken message becomes
  `VcsConflictException`. Merge conflicts and invalid transitions use
  `VcsConflictException`.
- **Azure DevOps.** A 400 on merge or completion becomes
  `VcsConflictException`. Other 400 responses become `VcsProviderException`.
  A 405 is not specially mapped and becomes `VcsProviderException`. Throttling
  maps to `VcsRateLimitException` with `RetryAfter` when Azure DevOps supplies
  it.
- **Gitea.** A 405 or 423 becomes `VcsConflictException`, not
  `VcsUnsupportedOperationException`. A 403 with a retry delay becomes
  `VcsRateLimitException`.

### Handle errors

```csharp
try
{
    await provider.MergePullRequestAsync(
        repository,
        number,
        new MergePullRequestRequest { Method = MergeMethod.Squash });
}
catch (PullRequestNotFoundException)
{
    // The pull request was removed or the number is wrong.
}
catch (VcsConflictException exception)
{
    // Merge conflict, stale source commit, or invalid transition.
    Console.WriteLine(exception.Message);
}
catch (VcsUnsupportedOperationException exception)
{
    // The provider or repository settings do not allow this method.
    Console.WriteLine($"{exception.Provider}: {exception.Operation}");
}
catch (VcsRateLimitException exception)
{
    // Wait for RetryAfter when provided.
    await Task.Delay(exception.RetryAfter ?? TimeSpan.FromSeconds(30));
}
catch (VcsAuthenticationException)
{
    // Supply a new token.
}
catch (VcsAuthorizationException)
{
    // Request more permission.
}
catch (VcsProviderException exception)
{
    // Other provider or transport failure.
    Console.WriteLine(exception.Message);
}
```

### Retry safety

| Operation | Automatically retried by `RetryLayer`? |
| --- | --- |
| Get repository, list repositories, repository exists | Yes |
| Get pull request, list pull requests | Yes |
| Close pull request | Yes, because close is idempotent |
| Create repository, delete repository | No |
| Create pull request | No |
| Merge pull request | No |

See [Layering](layers.md) for retry options and the retry predicate.

## Security

Error messages include the provider, operation, and a safe resource
identifier. They never include tokens, authorization headers, or full request
bodies. Provider SDK and raw HTTP exception types never escape a provider
package; they are preserved as the inner exception of a core exception.
