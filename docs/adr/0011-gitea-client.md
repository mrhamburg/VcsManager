# ADR-0011: Gitea uses REST API v1 with HttpClient

- Status: Accepted
- Date: 2026-10-25

## Context

Gitea is a self-hosted Git service. Every Gitea instance has a different host,
and there is no official .NET SDK that VcsManager can pin and support. Gitea does
publish a stable REST API, versioned under `/api/v1`, with a Swagger document.
Using an unofficial third-party client would add an unmaintained dependency and
a second error model.

The GitLab provider already establishes the pattern for a REST-only provider
(ADR-0004). Gitea fits the same shape, but with different pagination headers,
token authentication, and merge vocabulary.

## Decision

The `VcsManager.Gitea` package uses **the Gitea REST API v1 with `HttpClient`
and `System.Text.Json`** behind an internal `IGiteaApiClient` interface.

- All requests use the configured `BaseUrl` plus the `/api/v1` path. The
  provider normalizes a trailing slash and an existing `/api/v1` suffix so the
  path is never doubled.
- Authentication uses the `Authorization: token <token>` header. The token is
  supplied by the caller (ADR-0002) and is never logged or included in
  exception messages.
- Pagination uses `page` and `limit`. Gitea caps `limit` at 50, so the provider
  sends the effective page size and reports it in `PagedResult.PageSize`.
  `HasNextPage` comes from the `Link` response header with `rel="next"`, and
  `TotalCount` comes from `X-Total-Count` when Gitea supplies it.
- Repository and pull request responses are converted to core records inside the
  provider. No Gitea DTO appears in the public API.
- HTTP 401, 403, 404, 409, 422, 423, 429, and 5xx responses map to the core
  exception contract (ADR-0009, ADR-0010).
- A Gitea pull request that is merged has `state = closed` and `merged = true`.
  The provider checks `merged` before `state`.
- Draft pull requests use the Gitea `WIP:` title convention because
  `CreatePullRequestOption` has no draft field.

No third-party Gitea SDK is added. This decision is final for v1; changing the
client approach requires a new ADR.

## Consequences

- The provider has no third-party runtime dependency beyond the .NET base class
  library.
- Pagination and error handling stay consistent with the GitLab provider.
- Gitea server versions can change fields or endpoints. The provider tests use a
  fake client, and the opt-in integration tests validate a real instance.
- The self-hosted base URL is required and has no default.

## References

- Gitea API documentation: <https://docs.gitea.com/api/1.0/>
- Gitea Swagger document: <https://gitea.com/swagger.v1.json>
- Gitea repository endpoints: <https://docs.gitea.com/api/1.0/#tag/repository>
- Gitea pull request endpoints: <https://docs.gitea.com/api/1.0/#tag/pull-request>
- Gitea API usage and pagination: <https://docs.gitea.com/development/api-usage>
