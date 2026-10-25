# Gitea provider

The `VcsManager.Gitea` package implements the VcsManager provider contract for
self-hosted Gitea. It uses the Gitea REST API v1 with `HttpClient` and
`System.Text.Json`, so callers never handle HTTP response types.

## Install

```bash
dotnet add package VcsManager.Gitea
```

## Configure

Gitea uses access token authentication. VcsManager never reads a token from an
environment variable, credential store, or prompt unless you pass it through one
of the configuration sources below.

### Fluent API

```csharp
using VcsManager;
using VcsManager.Builder;
using VcsManager.Gitea;

var provider = new VcsProviderBuilder()
    .WithGitea(options =>
    {
        options.BaseUrl = Environment.GetEnvironmentVariable("VCS_GITEA_BASE_URL");
        options.Token = Environment.GetEnvironmentVariable("VCS_GITEA_TOKEN");
        options.DefaultOwner = "my-owner";
    })
    .Build();
```

### Environment variables

```bash
VCS_PROVIDER=gitea
VCS_GITEA_BASE_URL=https://gitea.example.com
VCS_GITEA_TOKEN=<token>
VCS_GITEA_DEFAULT_OWNER=my-owner
```

`VCS_{PROVIDER}_{KEY}` maps to a setting key, and underscores after the provider
name become dots. For example, `VCS_GITEA_BASE_URL` becomes `base.url`.

### YAML

```yaml
provider:
  gitea:
    base:
      url: https://gitea.example.com
    token: <token>
    default:
      owner: my-owner
```

```csharp
var provider = new VcsProviderBuilder()
    .WithGitea()
    .FromYamlFile("vcs.yaml")
    .Build();
```

Registration and configuration can be split. Call `WithGitea()` to register the
factory, then supply the selection and settings through `FromEnvironment`,
`FromYaml`, `FromYamlFile`, or `UseProvider`.

### Configuration keys

| Key | Required | Default | Description |
| --- | --- | --- | --- |
| `base.url` | Yes | — | Gitea server URL. |
| `token` | Yes | — | Access token. |
| `default.owner` | No | `null` | User or organization used when a request does not specify an owner. |

The base URL is required because every Gitea instance has a different host. Do
not include `/api/v1`; the provider adds the `/api/v1` path and normalizes a
trailing slash or an existing `/api/v1` suffix.

```csharp
var provider = new VcsProviderBuilder()
    .WithGitea(options =>
    {
        options.BaseUrl = "https://gitea.example.com";
        options.Token = token;
    })
    .Build();
```

The token is sent in the `Authorization: token <token>` header. VcsManager does
not log or include it in exception messages.

## Token scopes and permissions

Create an access token in **Settings > Applications**. The minimum scopes are:

| Operation | Minimum scopes |
| --- | --- |
| List repositories, get a repository, check a repository | `read:repository`; `read:user` and `read:organization` for owner-scoped listing |
| Create or delete a repository | `write:repository`; `write:organization` for organization repositories |
| List pull requests, get a pull request | `read:repository` |
| Create, merge, or close a pull request | `write:repository` |

A token created with the `all` scope works for every operation. The result is
always limited to repositories the token can access.

## Repository addressing

Gitea repositories use `{owner}/{name}`. The mapping is:

| Field | Meaning |
| --- | --- |
| `Owner` | User or organization. Optional: when absent, `DefaultOwner` is used. |
| `Project` | Unsupported. Gitea has no project or subgroup level. |
| `Name` | Repository name. |

The provider URL-encodes owner and repository names. A request that sets
`Project` is rejected with a validation error instead of silently ignoring it.

## List repositories

`ListRepositoriesAsync` selects the endpoint from the owner:

- With an organization owner, it lists `GET /orgs/{owner}/repos`.
- With a user owner, it resolves the user ID and lists with
  `GET /repos/search?uid={id}`.
- Without an owner, it lists `GET /repos/search` for repositories visible to the
  token.

Supported filters:

| Filter | Gitea behavior |
| --- | --- |
| `NameContains` | Sent as `q` for search endpoints, and applied to the returned page for organization listings. |
| `IncludeArchived` | Sent as `archived=false` by default. Archived repositories are included when the value is `true`. |
| `Visibility` | Sent as `is_private=true` for `Private` and `is_private=false` for `Public`. `Internal` is rejected with a validation error. |

`PagedResult.PageSize` is the effective page size. Gitea caps `limit` at 50, so
a request for more than 50 returns 50 items per page and reports `PageSize = 50`.
`HasNextPage` comes from the `Link` header with `rel="next"`, and `TotalCount`
comes from `X-Total-Count` when Gitea supplies it. The provider fetches one page;
the caller controls paging.

Gitea has no internal repository visibility. Only `Public` and `Private` are
available.

## Repository operations

`CreateRepositoryAsync` requires an owner from `Owner` or `DefaultOwner`. It uses
`POST /orgs/{org}/repos` for an organization and `POST /user/repos` for a user.
The user endpoint creates the repository under the authenticated token's account.
When the owner is a user, the provider verifies that the owner is the token user
and throws `VcsUnsupportedOperationException` for any other user.

`Description`, `DefaultBranch`, and `InitializeWithReadme` map to the Gitea
create-repository fields. `InitializeWithReadme` creates an initial commit.
`Public` maps to `private = false`, `Private` maps to `private = true`, and
`Internal` is rejected with a validation error.

`GetRepositoryAsync`, `RepositoryExistsAsync`, and `DeleteRepositoryAsync` use
`GET` and `DELETE /repos/{owner}/{repo}`. `RepositoryExistsAsync` returns `false`
for a missing repository and maps other failures normally.

> **Warning:** `DeleteRepositoryAsync` permanently deletes the repository and
> its history. The operation cannot be undone. Deletion is not idempotent: a
> second delete returns `RepositoryNotFoundException`.

## Pull requests

Gitea addresses pull requests by a numeric repository-scoped index. The index is
`PullRequest.Number`.

`CreatePullRequestAsync` maps the source branch, target branch, title, and
description. `CreatePullRequestOption` has no draft field, so the provider
prefixes the title with `WIP:` when `Draft` is `true`. A title that already
starts with `WIP:` is not changed.

`GetPullRequestAsync` and `ListPullRequestsAsync` return the same normalized
`PullRequest` model. List filters map as follows:

| Filter | Gitea behavior |
| --- | --- |
| `State` | Sent as `open`, `closed`, or `all`. `Merged` uses `closed` and is filtered on the returned page. |
| `Author` | Sent as `poster`. |
| `TargetBranch` | Sent as `base_branch`. |
| `SourceBranch` | Applied to the returned page. Gitea has no source-branch list parameter. |

A merged pull request has `state = closed` and `merged = true`. The provider
checks the `merged` flag before the state, so it reports
`PullRequestState.Merged`.

| Gitea response | `PullRequestState` |
| --- | --- |
| `state = open`, not draft | `Open` |
| `state = open`, `draft = true` or a `WIP:` title | `Draft` |
| `state = closed`, `merged = false` | `Closed` |
| `state = closed`, `merged = true` | `Merged` |

`PagedResult.PageSize` is capped at 50 in the same way as repository listing.
Approvals, review requests, and reviewer votes are not part of v1.

## Merge methods

| `MergeMethod` | Gitea `do` value |
| --- | --- |
| `Merge` | `merge` |
| `Squash` | `squash` |
| `Rebase` | `rebase` |

`MergePullRequestAsync` calls
`POST /repos/{owner}/{repo}/pulls/{index}/merge` and then reads the pull request
again to return its merged state.

- `DeleteSourceBranch` maps to `delete_branch_after_merge`.
- `CommitTitle` and `CommitMessage` map to `merge_title_field` and
  `merge_message_field`.
- Repository settings control success. `Merge` requires
  `allow_merge_commits`; `Squash` requires `allow_squash_merge`; `Rebase`
  requires `allow_rebase` or `allow_rebase_explicit`. The provider never changes
  repository settings.
- A disabled merge style, a conflict, an archived repository, or an invalid
  transition returns `VcsConflictException`.
- A pull request that is already merged is returned unchanged. A closed pull
  request cannot be merged and returns `VcsConflictException`.
- A missing pull request returns `PullRequestNotFoundException`.

Gitea also supports `rebase-merge`, `fast-forward-only`, and `manually-merged`
merge styles. They are not part of the v1 mapping.

`ClosePullRequestAsync` never merges. It sets `state = closed` with
`PATCH /repos/{owner}/{repo}/pulls/{index}`. It reads the pull request first and
skips the write when the pull request is already closed or merged, so close is
idempotent. A merged pull request is returned in `PullRequestState.Merged`.

## Error mapping

| Gitea response | VcsManager exception |
| --- | --- |
| 401 Unauthorized | `VcsAuthenticationException` |
| 403 Forbidden | `VcsAuthorizationException` |
| 404 Not Found | `RepositoryNotFoundException` or `PullRequestNotFoundException` |
| 405 Method Not Allowed | `VcsConflictException` |
| 409 Conflict | `VcsConflictException` |
| 422 Unprocessable Entity | `VcsConflictException` |
| 423 Locked (archived repository) | `VcsConflictException` |
| 429 Too Many Requests | `VcsRateLimitException` with `RetryAfter` when Gitea supplies it |
| 5xx | `VcsProviderException` |

## Code sample

This is the same flow used by the sample console app. It assumes the branches
`sample/merge` and `sample/close` already exist.

```csharp
using VcsManager;
using VcsManager.Builder;
using VcsManager.Gitea;

var provider = new VcsProviderBuilder()
    .WithGitea(options =>
    {
        options.BaseUrl = Environment.GetEnvironmentVariable("VCS_GITEA_BASE_URL");
        options.Token = Environment.GetEnvironmentVariable("VCS_GITEA_TOKEN");
        options.DefaultOwner = Environment.GetEnvironmentVariable("VCS_GITEA_DEFAULT_OWNER");
    })
    .Build();

await using (provider)
{
    var repositoryName =
        $"vcsmanager-sample-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..48];
    var repository = new RepositoryRef { Name = repositoryName };

    var created = await provider.CreateRepositoryAsync(new CreateRepositoryRequest
    {
        Name = repository.Name,
        InitializeWithReadme = true,
        DefaultBranch = "main",
    });
    Console.WriteLine($"Created repository: {created.FullName}");

    var repositories = await provider.ListRepositoriesAsync(
        new RepositoryQuery { NameContains = repositoryName },
        new PaginationOptions { PageSize = 10 });
    foreach (var item in repositories.Items)
    {
        Console.WriteLine($"  - {item.FullName}");
    }

    var first = await provider.CreatePullRequestAsync(repository, new CreatePullRequestRequest
    {
        Title = "Sample: merge a pull request",
        SourceBranch = "sample/merge",
        TargetBranch = "main",
    });
    var merged = await provider.MergePullRequestAsync(repository, first.Number);
    Console.WriteLine($"Merged pull request #{merged.Number}: {merged.State}");

    var second = await provider.CreatePullRequestAsync(repository, new CreatePullRequestRequest
    {
        Title = "Sample: close a pull request",
        SourceBranch = "sample/close",
        TargetBranch = "main",
    });
    var closed = await provider.ClosePullRequestAsync(repository, second.Number);
    Console.WriteLine($"Closed pull request #{closed.Number}: {closed.State}");

    await provider.DeleteRepositoryAsync(repository);
    Console.WriteLine($"Deleted repository: {created.FullName}");
}
```

## Not supported in v1

- Project-scoped repository addressing.
- Internal repository visibility.
- Pull request approvals, review requests, and reviewer votes.
- Member and team management.
- Repository transfer, rename, and archive changes.
- The `rebase-merge`, `fast-forward-only`, and `manually-merged` merge styles.
- OAuth and Gitea Actions authentication.

## References

- Gitea API documentation: [https://docs.gitea.com/api/1.0/](https://docs.gitea.com/api/1.0/)
- Gitea API usage and authentication: [https://docs.gitea.com/development/api-usage](https://docs.gitea.com/development/api-usage)
- Gitea repository API: [https://docs.gitea.com/api/1.0/#tag/repository](https://docs.gitea.com/api/1.0/#tag/repository)
- Gitea pull request API: [https://docs.gitea.com/api/1.0/#tag/pull-request](https://docs.gitea.com/api/1.0/#tag/pull-request)
- Gitea access tokens: [https://docs.gitea.com/development/oauth2-provider](https://docs.gitea.com/development/oauth2-provider)
- Gitea merge styles: [https://docs.gitea.com/usage/repositories](https://docs.gitea.com/usage/repositories)
