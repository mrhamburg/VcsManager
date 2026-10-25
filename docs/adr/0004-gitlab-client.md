# ADR-0004: GitLab uses REST API v4 with HttpClient

- Status: Accepted
- Date: 2026-10-25

## Context

GitLab exposes a stable REST API v4 and also a GraphQL API. The operations
needed by v1 are simple resource CRUD calls. A third-party GitLab SDK would add
its own release cadence and models, and GraphQL would add query complexity that
v1 does not need.

## Decision

The `VcsManager.GitLab` package calls the **GitLab REST API v4** directly with
`HttpClient`.

- The provider defines an internal `IGitLabApiClient` interface. Its
  implementation sends REST v4 requests and deserializes responses with
  `System.Text.Json`.
- `HttpClient` is supplied by the caller or the provider factory. The provider
  sets the base address and the `PRIVATE-TOKEN` header; it does not create a
  global static client.
- GitLab JSON models stay internal and are mapped to core records.
- Pagination follows the `page` and `per_page` query parameters and the
  `X-Total`, `X-Page`, `X-Next-Page`, and `X-Prev-Page` response headers.
- Unit tests use a fake `HttpMessageHandler`; no real GitLab instance is needed.

## Consequences

- The provider owns its request, serialization, and error-mapping code, which is
  more code than an SDK wrapper but has no third-party API coupling.
- GitLab.com and self-managed GitLab use the same API version. The base URL is
  configuration, not a constant.
- GitLab features that exist only in GraphQL are out of scope for v1.
- REST v4 response changes are handled in one internal client and covered by
  provider tests.

## References

- GitLab REST API: [https://docs.gitlab.com/api/rest/](https://docs.gitlab.com/api/rest/)
- GitLab merge requests API: [https://docs.gitlab.com/api/merge_requests/](https://docs.gitlab.com/api/merge_requests/)
- GitLab projects API: [https://docs.gitlab.com/api/projects/](https://docs.gitlab.com/api/projects/)
- GitLab pagination: [https://docs.gitlab.com/api/rest/#pagination](https://docs.gitlab.com/api/rest/#pagination)
