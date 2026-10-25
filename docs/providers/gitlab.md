# GitLab provider

The `VcsManager.GitLab` package implements the VcsManager provider contract for
GitLab.com and self-managed GitLab. It calls the GitLab REST API v4 with
`HttpClient`, so callers never handle HTTP response types.

## Install

```bash
dotnet add package VcsManager.GitLab
```

## Configure

GitLab uses personal access token (PAT) authentication. VcsManager never reads
a token from an environment variable, credential store, or prompt unless you
pass it through one of the configuration sources below.

### Fluent API

```csharp
using VcsManager;
using VcsManager.Builder;
using VcsManager.GitLab;

var provider = new VcsProviderBuilder()
    .WithGitLab(options =>
    {
        options.Token = Environment.GetEnvironmentVariable("VCS_GITLAB_TOKEN");
        options.DefaultNamespace = "my-group";
    })
    .Build();
```

### Environment variables

```bash
VCS_PROVIDER=gitlab
VCS_GITLAB_TOKEN=<token>
VCS_GITLAB_DEFAULT_NAMESPACE=my-group
```

`VCS_{PROVIDER}_{KEY}` maps to a setting key, and underscores after the provider
name become dots. For example, `VCS_GITLAB_DEFAULT_NAMESPACE` becomes
`default.namespace`.

### YAML

```yaml
provider:
  gitlab:
    token: <token>
    default:
      namespace: my-group
```

```csharp
var provider = new VcsProviderBuilder()
    .WithGitLab()
    .FromYamlFile("vcs.yaml")
    .Build();
```

Registration and configuration can be split. Call `WithGitLab()` to register
the factory, then supply the selection and settings through `FromEnvironment`,
`FromYaml`, `FromYamlFile`, or `UseProvider`.

### Configuration keys

| Key | Required | Default | Description |
| --- | --- | --- | --- |
| `token` | Yes | — | Personal access token. |
| `base.url` | No | `https://gitlab.com` | GitLab base URL. |
| `default.namespace` | No | `null` | Namespace used when a request does not specify an owner. |

## Self-managed GitLab

Set `BaseUrl` to the server URL. Do not include `/api/v4`; the provider adds the
`/api/v4` path.

```csharp
var provider = new VcsProviderBuilder()
    .WithGitLab(options =>
    {
        options.Token = token;
        options.BaseUrl = "https://gitlab.example.com";
    })
    .Build();
```

or:

```bash
VCS_PROVIDER=gitlab
VCS_GITLAB_TOKEN=<token>
VCS_GITLAB_BASE_URL=https://gitlab.example.com
```

## Token scopes and permissions

Create a personal access token with the `api` scope for the full provider API.
A token with the `read_api` scope can only read and list projects and merge
requests.

| Operation | Minimum scope |
| --- | --- |
| List projects, get a project, get a merge request | `read_api` |
| Create or delete a project | `api`, with the Owner or Maintainer role required by project settings |
| Create, merge, or close a merge request | `api`, with at least the Developer role on the project |

The token is sent in the `PRIVATE-TOKEN` header. VcsManager does not log or
include it in exception messages.

## Repository addressing

GitLab projects use `{owner}/{project}/{name}`. The mapping is:

| Field | Meaning |
| --- | --- |
| `Owner` | Top-level namespace. Required unless `DefaultNamespace` is configured. |
| `Project` | Optional subgroup path, for example `subgroup/team`. |
| `Name` | Project path. |

Create operations resolve `{owner}/{project}` to a namespace and create the
project below it. Nested namespace separators are URL-encoded as `%2F` when the
provider calls the project API.

## List projects

`ListRepositoriesAsync` returns projects visible to the token:

- With `RepositoryQuery.Owner` or `DefaultNamespace`, it lists group projects
  with `include_subgroups=true`. `RepositoryQuery.Project` narrows the scope to
  a subgroup.
- Without a namespace, it lists projects visible to the authenticated user.
- `Visibility` and `NameContains` map to the GitLab `visibility` and `search`
  filters.
- Archived projects are excluded unless `IncludeArchived` is `true`.
- `TotalCount` comes from the `X-Total` response header.
- `HasNextPage` comes from the `X-Next-Page` response header, not from the item
  count.
- The provider fetches exactly one page. The caller controls paging.

## Repository operations

`CreateRepositoryAsync` maps `Name`, `Description`, `Visibility`,
`InitializeWithReadme`, and `DefaultBranch` to the GitLab create-project API.
GitLab accepts `private`, `internal`, and `public` visibility; all three map
directly.

`GetRepositoryAsync`, `RepositoryExistsAsync`, and `DeleteRepositoryAsync`
accept a namespace, an optional subgroup, and a project path.
`RepositoryExistsAsync` returns `false` for a missing project and maps other
failures normally.

> **Warning:** `DeleteRepositoryAsync` permanently deletes the project and its
> repository, issues, merge requests, and settings. The operation cannot be
> undone. GitLab also has delayed project deletion on some plans; a deleted
> project can remain recoverable for a configured period. Deletion is not
> idempotent: a second delete returns `RepositoryNotFoundException`.

## Merge requests

GitLab calls pull requests merge requests. VcsManager uses the project-scoped
IID as `PullRequest.Number`.

`CreatePullRequestAsync` maps the source and target branches, title, and
description. GitLab has no draft flag on the create endpoint, so the provider
prefixes the title with `Draft: ` when `Draft` is `true`. A title that already
starts with `Draft:` or `WIP:` is not changed.

`GetPullRequestAsync` and `ListPullRequestsAsync` return the same normalized
`PullRequest` model. List filters map as follows:

| Filter | GitLab behavior |
| --- | --- |
| `State` | Sent as `opened`, `closed`, `merged`, or `all`; draft and open merge requests are separated on the returned page. |
| `Author` | Sent as `author_username`. |
| `SourceBranch` | Sent as `source_branch`. |
| `TargetBranch` | Sent as `target_branch`. |

`PullRequestState.Draft` is set from the GitLab `draft` or `work_in_progress`
flag, or from a `Draft:`/`WIP:` title. `PullRequestState.Merged` uses the
GitLab `merged` state and `merged_at` timestamp.

## Merge methods

| `MergeMethod` | GitLab behavior |
| --- | --- |
| `Merge` | Merges with the project `merge_method`. |
| `Squash` | Sends `squash = true`. The project squash setting can still override the result. |
| `Rebase` | Rejected with `VcsUnsupportedOperationException`. |

GitLab controls merge behavior with project settings, not per-request switches.
The provider never changes project settings. `Merge` and `Squash` are sent per
request, but the project setting can still force or forbid squashing.

`Rebase` is not available in v1. The provider cannot prove that the project uses
`rebase_merge` or `ff`, so it rejects the request instead of silently producing
a merge commit. Use the GitLab API directly when you need rebase behavior.

`DeleteSourceBranch` maps to `should_remove_source_branch`. Project settings can
still change whether GitLab removes the branch. `CommitTitle` and
`CommitMessage` map to the merge commit message; for squash merges the same
message also becomes the squash commit message.

A failed merge returns `VcsConflictException` for HTTP 405, 409, and 422. A
missing merge request returns `PullRequestNotFoundException`.

`ClosePullRequestAsync` never merges. It reads the merge request first and skips
the write when the merge request is already closed or merged, so close is
idempotent. A merged merge request is returned in `PullRequestState.Merged`.

## Code sample

This is the same flow used by the sample console app. It assumes the branches
`sample/merge` and `sample/close` already exist in the project.

```csharp
using VcsManager;
using VcsManager.Builder;
using VcsManager.GitLab;

var provider = new VcsProviderBuilder()
    .WithGitLab(options =>
    {
        options.Token = Environment.GetEnvironmentVariable("VCS_GITLAB_TOKEN");
        options.DefaultNamespace = Environment.GetEnvironmentVariable("VCS_GITLAB_NAMESPACE");
    })
    .Build();

await using (provider)
{
    var repositoryName =
        $"vcsmanager-sample-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..48];
    var repository = new RepositoryRef
    {
        Name = repositoryName,
        Owner = Environment.GetEnvironmentVariable("VCS_GITLAB_NAMESPACE"),
    };

    var created = await provider.CreateRepositoryAsync(new CreateRepositoryRequest
    {
        Name = repository.Name,
        Owner = repository.Owner,
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
        Title = "Sample: merge a merge request",
        SourceBranch = "sample/merge",
        TargetBranch = "main",
    });
    var merged = await provider.MergePullRequestAsync(repository, first.Number);
    Console.WriteLine($"Merged merge request !{merged.Number}: {merged.State}");

    var second = await provider.CreatePullRequestAsync(repository, new CreatePullRequestRequest
    {
        Title = "Sample: close a merge request",
        SourceBranch = "sample/close",
        TargetBranch = "main",
    });
    var closed = await provider.ClosePullRequestAsync(repository, second.Number);
    Console.WriteLine($"Closed merge request !{closed.Number}: {closed.State}");

    await provider.DeleteRepositoryAsync(repository);
    Console.WriteLine($"Deleted repository: {created.FullName}");
}
```

## Not supported in v1

- OAuth authentication and GitLab App authentication.
- Pull request approvals, reviewer assignment, and merge trains.
- Member management.
- Changing project merge settings on the caller's behalf.
- The `Rebase` merge method, because project settings cannot be verified per
  call.
- GraphQL-only operations.

## References

- GitLab projects API: <https://docs.gitlab.com/api/projects/>
- GitLab merge requests API: <https://docs.gitlab.com/api/merge_requests/>
- GitLab merge methods: <https://docs.gitlab.com/user/project/merge_requests/methods/>
- GitLab REST pagination: <https://docs.gitlab.com/api/rest/#pagination>
- GitLab personal access tokens: <https://docs.gitlab.com/user/profile/personal_access_tokens/>
