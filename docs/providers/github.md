# GitHub provider

The `VcsManager.GitHub` package implements the VcsManager provider contract for
GitHub.com and GitHub Enterprise Server. It uses Octokit 14.0.0 behind an
internal client, so callers never handle Octokit types.

## Install

```bash
dotnet add package VcsManager.GitHub
```

## Configure

GitHub uses personal access token (PAT) authentication. VcsManager never reads
a token from an environment variable, credential store, or prompt unless you
pass it through one of the configuration sources below.

### Fluent API

```csharp
using VcsManager;
using VcsManager.Builder;
using VcsManager.GitHub;

var provider = new VcsProviderBuilder()
    .WithGitHub(options =>
    {
        options.Token = Environment.GetEnvironmentVariable("VCS_GITHUB_TOKEN");
        options.DefaultOwner = "my-org";
    })
    .Build();
```

### Environment variables

```bash
VCS_PROVIDER=github
VCS_GITHUB_TOKEN=<token>
VCS_GITHUB_DEFAULT_OWNER=my-org
```

`VCS_{PROVIDER}_{KEY}` maps to a setting key, and underscores after the provider
name become dots. For example, `VCS_GITHUB_DEFAULT_OWNER` becomes
`default.owner`.

### YAML

```yaml
provider:
  github:
    token: <token>
    default:
      owner: my-org
```

```csharp
var provider = new VcsProviderBuilder()
    .WithGitHub()
    .FromYamlFile("vcs.yaml")
    .Build();
```

Registration and configuration can be split. Call `WithGitHub()` to register
the factory, then supply the selection and settings through `FromEnvironment`,
`FromYaml`, `FromYamlFile`, or `UseProvider`.

### Configuration keys

| Key | Required | Default | Description |
| --- | --- | --- | --- |
| `token` | Yes | — | Personal access token. |
| `base.url` | No | `https://api.github.com` | GitHub REST API base URL. |
| `default.owner` | No | `null` | Owner or organization used when a request does not specify one. |
| `request.timeout` | No | `00:01:40` | Timeout for one GitHub HTTP request. Use the `hh:mm:ss` time span format. The value must be positive. |

`GitHubOptions.RequestTimeout` bounds one GitHub HTTP request and defaults to
100 seconds. Set it with the `request.timeout` key in environment variables,
YAML, or `UseProvider`. The fluent `WithGitHub` method writes the same key. A
request that exceeds the timeout fails with a transient `VcsProviderException`. Octokit does not accept a cancellation token on every operation, so
caller cancellation is best-effort. The request timeout limits in-flight work
when a timeout layer expires or the caller cancels.

## GitHub Enterprise Server

Set `BaseUrl` to the server REST API URL, including the API path:

```csharp
var provider = new VcsProviderBuilder()
    .WithGitHub(options =>
    {
        options.Token = token;
        options.BaseUrl = "https://github.example.com/api/v3";
    })
    .Build();
```

or:

```bash
VCS_PROVIDER=github
VCS_GITHUB_TOKEN=<token>
VCS_GITHUB_BASE_URL=https://github.example.com/api/v3
```

## Token scopes and permissions

The minimum permissions depend on the operation and the repository visibility.

### Classic personal access tokens

| Operation | Minimum scope |
| --- | --- |
| Read private repositories, list repositories, get a repository | `repo` |
| Create or read public repositories only | `public_repo` |
| Create, read, and list private repositories | `repo` |
| Create a pull request, merge a pull request, close a pull request | `repo` for private repositories, `public_repo` for public repositories |
| Delete a repository | `delete_repo` plus `repo` for private repositories |

### Fine-grained personal access tokens

Grant the token access to the target repositories or organization. Then set
these repository permissions:

| Permission | Access | Used for |
| --- | --- | --- |
| Metadata | Read | Required by GitHub for every fine-grained token. |
| Contents | Read and write | Repository contents, initial readme, repository seeding, and branch deletion. |
| Administration | Read and write | Repository creation and deletion. |
| Pull requests | Read and write | Creating, listing, merging, and closing pull requests. |

A fine-grained token can see only the repositories it is granted. Listing
returns that visible subset even when the owner has more repositories.

## Repository addressing

GitHub repositories use `{owner}/{name}`. `RepositoryRef.Owner` is required for
get, delete, and exists operations unless `DefaultOwner` is configured. Owner and
repository names may contain only letters, digits, `.`, `_`, and `-`. The values
`.` and `..` are not valid. The provider rejects other values with an
`ArgumentException` before it sends a request. The provider trims leading and
trailing whitespace from names, pull request titles, and branch names before it
sends them.
`RepositoryRef.Project` is not meaningful on GitHub and is rejected with an
`ArgumentException` instead of being ignored.

Create operations use the authenticated user when neither the request nor
`DefaultOwner` supplies an owner. When an owner is supplied, the repository is
created in that organization.

## List repositories

`ListRepositoriesAsync` returns repositories visible to the token:

- With `RepositoryQuery.Owner` or `DefaultOwner`, it lists repositories in that
  organization.
- Without an owner, it lists repositories for the authenticated user, including
  repositories the user can reach through organization membership.
- `Visibility` is passed to GitHub when the endpoint supports it.
- `NameContains` and `IncludeArchived` are applied to the returned page.
- `TotalCount` is `null` because GitHub does not report it for these endpoints.
- `HasNextPage` comes from the GitHub `Link` header, not from the item count.
- The provider fetches exactly one page. The caller controls paging.

## Repository operations

`CreateRepositoryAsync` maps description, visibility, `InitializeWithReadme`,
and `DefaultBranch`. GitHub does not accept a default branch in the create
request, so the provider updates it after creation when the new repository
already has a branch. An empty repository has no branch to select. `Internal`
visibility is supported only on GitHub plans and servers that allow it; when
GitHub rejects it, the provider throws `VcsUnsupportedOperationException`.

`SeedRepositoryAsync` writes one initial commit with all supplied files through
the GitHub Git data API. The repository must be empty. The seeded branch becomes
the default branch. The token needs Contents write permission. The provider does
not preserve executable bits, symlinks, submodules, or empty directories.

`GetRepositoryAsync`, `RepositoryExistsAsync`, and `DeleteRepositoryAsync`
accept an owner and name. `RepositoryExistsAsync` returns `false` for a missing
repository and maps other failures normally.

> **Warning:** `DeleteRepositoryAsync` permanently deletes the repository,
> including its issues, pull requests, releases, and settings. The operation
> cannot be undone. Delete requires repository administration permission and the
> classic `delete_repo` scope or fine-grained Administration write permission.
> Deletion is not idempotent: a second delete returns
> `RepositoryNotFoundException`.

## Pull requests

`CreatePullRequestAsync` maps the source branch to GitHub `head`, the target
branch to `base`, the description to the pull request body, and `Draft` to a
draft pull request. Both branches must already exist. An invalid base branch or
a missing branch maps to `VcsConflictException`.

`GetPullRequestAsync` and `ListPullRequestsAsync` return the same normalized
`PullRequest` model. List filters map as follows:

| Filter | GitHub behavior |
| --- | --- |
| `State` | Open, draft, merged, and closed states are mapped from GitHub state, draft flag, and merge timestamp. |
| `Author` | Filtered on the returned page. |
| `SourceBranch` | Filtered on the returned page. |
| `TargetBranch` | Sent as the GitHub `base` filter. |

Draft pull requests set `PullRequestState.Draft` and `IsDraft`. A merged pull
request sets `PullRequestState.Merged` and `MergedAt`, even though GitHub
reports its state as `closed`.

## Merge methods

| `MergeMethod` | GitHub `merge_method` |
| --- | --- |
| `Merge` | `merge` |
| `Squash` | `squash` |
| `Rebase` | `rebase` |

The repository must allow the requested method. Branch protection rules,
required checks, and merge queues can still reject the merge. A rejected method
or blocked merge maps to `VcsUnsupportedOperationException` or
`VcsConflictException`; the provider never falls back to another method.
`CommitTitle` and `CommitMessage` are passed to GitHub.

`DeleteSourceBranch` deletes the source branch after a successful merge by
removing its Git reference. GitHub does not delete the branch as part of the
merge call. A branch that belongs to a fork is not deleted. Branch protection
can prevent deletion; that failure is surfaced to the caller and is not hidden.

`ClosePullRequestAsync` never merges. It reads the pull request first and skips
the write when the pull request is already closed or merged, so close is
idempotent. A merged pull request is returned in `PullRequestState.Merged`.

## Code sample

This is the same flow used by the sample console app. It assumes the branches
`sample/merge` and `sample/close` already exist in the repository.

```csharp
using VcsManager;
using VcsManager.Builder;
using VcsManager.GitHub;

var provider = new VcsProviderBuilder()
    .WithGitHub(options =>
    {
        options.Token = Environment.GetEnvironmentVariable("VCS_GITHUB_TOKEN");
        options.DefaultOwner = Environment.GetEnvironmentVariable("VCS_GITHUB_OWNER");
    })
    .Build();

await using (provider)
{
    var repositoryName =
        $"vcsmanager-sample-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..48];
    var repository = new RepositoryRef
    {
        Name = repositoryName,
        Owner = Environment.GetEnvironmentVariable("VCS_GITHUB_OWNER"),
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

- GitHub App authentication and OAuth.
- Pull request approvals, review submission, and merge queues.
- Organization member management.
- Project-scoped repository addressing (`RepositoryRef.Project`).
- Provider-reported total counts for repository and pull request listings.

## References

- GitHub REST API for repositories: [https://docs.github.com/en/rest/repos/repos](https://docs.github.com/en/rest/repos/repos)
- GitHub REST API for pull requests: [https://docs.github.com/en/rest/pulls/pulls](https://docs.github.com/en/rest/pulls/pulls)
- GitHub fine-grained personal access tokens: [https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens)
- GitHub Enterprise Server REST API: [https://docs.github.com/en/enterprise-server@latest/rest](https://docs.github.com/en/enterprise-server@latest/rest)
- Octokit.NET: [https://github.com/octokit/octokit.net](https://github.com/octokit/octokit.net)
