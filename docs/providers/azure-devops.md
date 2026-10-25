# Azure DevOps provider

The `VcsManager.AzureDevOps` package implements the VcsManager provider contract
for Azure DevOps Services. It uses the official
`Microsoft.TeamFoundationServer.Client` SDK behind an internal API client, with
REST API 7.1 as the fallback for operations the SDK cannot express. Callers never
handle SDK types or HTTP response types.

## Install

```bash
dotnet add package VcsManager.AzureDevOps
```

## Configure

Azure DevOps uses personal access token (PAT) authentication. VcsManager never
reads a token from an environment variable, credential store, or prompt unless
you pass it through one of the configuration sources below.

### Fluent API

```csharp
using VcsManager;
using VcsManager.Builder;
using VcsManager.AzureDevOps;

var provider = new VcsProviderBuilder()
    .WithAzureDevOps(options =>
    {
        options.OrganizationUrl = Environment.GetEnvironmentVariable("VCS_AZUREDEVOPS_ORGANIZATION_URL");
        options.PersonalAccessToken = Environment.GetEnvironmentVariable("VCS_AZUREDEVOPS_PERSONAL_ACCESS_TOKEN");
        options.DefaultProject = "my-project";
    })
    .Build();
```

### Environment variables

```bash
VCS_PROVIDER=azuredevops
VCS_AZUREDEVOPS_ORGANIZATION_URL=https://dev.azure.com/my-organization
VCS_AZUREDEVOPS_PERSONAL_ACCESS_TOKEN=<token>
VCS_AZUREDEVOPS_DEFAULT_PROJECT=my-project
```

`VCS_{PROVIDER}_{KEY}` maps to a setting key, and underscores after the provider
name become dots. For example, `VCS_AZUREDEVOPS_ORGANIZATION_URL` becomes
`organization.url`.

### YAML

```yaml
provider:
  azuredevops:
    organization:
      url: https://dev.azure.com/my-organization
    personal:
      access:
        token: <token>
    default:
      project: my-project
```

```csharp
var provider = new VcsProviderBuilder()
    .WithAzureDevOps()
    .FromYamlFile("vcs.yaml")
    .Build();
```

Registration and configuration can be split. Call `WithAzureDevOps()` to
register the factory, then supply the selection and settings through
`FromEnvironment`, `FromYaml`, `FromYamlFile`, or `UseProvider`.

### Configuration keys

| Key | Required | Default | Description |
| --- | --- | --- | --- |
| `organization.url` | Yes | — | Organization URL. |
| `personal.access.token` | Yes | — | Personal access token. |
| `default.project` | No | `null` | Team project used when a request does not specify one. |
| `api.version` | No | `7.1` | REST API version used for fallback calls. |

The organization URL is one of:

- `https://dev.azure.com/{organization}`
- `https://{organization}.visualstudio.com`

The provider derives the organization name from the URL. It sends the PAT with
`VssBasicCredential` for the SDK and with HTTP Basic authentication for REST
fallback calls. VcsManager does not log the token or include it in exception
messages.

## Token scopes and permissions

Create a personal access token with the **Code** scope.

| Operation | Minimum scope |
| --- | --- |
| List repositories, get a repository, check a repository | Code (Read) |
| Create a repository | Code (Read & Write) |
| Delete a repository | Code (Read & Write), plus project administration where organization policy requires it |
| List pull requests, get a pull request | Code (Read) |
| Create, merge, or close a pull request | Code (Read & Write) |

Creating a repository needs an existing team project. VcsManager does not create
projects and does not request organization or project administration scopes.

## Repository addressing

Azure DevOps repositories use `{organization}/{project}/{name}`. The mapping is:

| Field | Meaning |
| --- | --- |
| `Owner` | Organization. Optional: when absent, the organization from `OrganizationUrl` is used. A different value is rejected. |
| `Project` | Team project. Optional: when absent, `DefaultProject` is used. |
| `Name` | Repository name. |

A repository reference must resolve to an organization and a team project.
When neither `Project` nor `DefaultProject` is set, repository operations that
need a project throw `ArgumentException`. Listing repositories is the exception:
without a project, the provider lists repositories in the organization.

Repository IDs are GUIDs. `VcsRepository.Id` contains the GUID string.

## List repositories

`ListRepositoriesAsync` returns repositories visible to the token:

- With `RepositoryQuery.Project` or `DefaultProject`, it lists repositories in
  that team project.
- Without either, it lists repositories across the organization. This is the
  REST API 7.1 organization-level repository listing and is limited to the one
  organization in `OrganizationUrl`.
- `NameContains` and `Visibility` are applied to the returned page. Azure DevOps
  has no repository archive flag, so `IncludeArchived` has no effect, and
  repositories are reported as `VcsVisibility.Private`.
- `TotalCount` is `null` because Azure DevOps does not report a total for this
  listing.
- `HasNextPage` is computed by requesting one item more than `PageSize`.
- The provider fetches exactly one page. The caller controls paging with
  `PaginationOptions.Page` and `PaginationOptions.PageSize`.

## Repository operations

`CreateRepositoryAsync` creates an empty repository in the resolved team
project. Azure DevOps creates the default branch on the first push, so
`DefaultBranch` is accepted but has no effect. `InitializeWithReadme` is not
supported and throws `VcsUnsupportedOperationException`.

`GetRepositoryAsync`, `RepositoryExistsAsync`, and `DeleteRepositoryAsync`
resolve the organization and team project with the same rules. They accept a
repository name; the provider reads the repository to obtain its GUID before
deletion. `RepositoryExistsAsync` returns `false` for a missing repository and
maps other failures normally.

> **Warning:** `DeleteRepositoryAsync` permanently deletes the repository and
> its history. The operation cannot be undone. Deletion is not idempotent: a
> second delete returns `RepositoryNotFoundException`.

## Pull requests

VcsManager calls Azure DevOps pull requests pull requests. The numeric
`pullRequestId` is `PullRequest.Number`.

`CreatePullRequestAsync` converts source and target branch names to
`refs/heads/{branch}` refs and maps the title, description, and draft flag.
Azure DevOps returns the created pull request immediately; branch policies do
not block creation.

`GetPullRequestAsync` and `ListPullRequestsAsync` return the same normalized
`PullRequest` model. List filters map as follows:

| Filter | Azure DevOps behavior |
| --- | --- |
| `State` | Sent as `Active`, `Abandoned`, `Completed`, or `All`. Draft and open pull requests share `Active`, so `Draft` is filtered after the response. |
| `Author` | Applied to the returned page. The SDK filter needs a creator ID, which a display name does not provide. |
| `SourceBranch` | Sent as `refs/heads/{branch}`. |
| `TargetBranch` | Sent as `refs/heads/{branch}`. |

`PullRequestState` maps from the Azure DevOps status and draft flag:

| Status | Draft | `PullRequestState` |
| --- | --- | --- |
| `Active` | No | `Open` |
| `Active` | Yes | `Draft` |
| `Abandoned` | — | `Closed` |
| `Completed` | — | `Merged` |

`TotalCount` is `null`, and `HasNextPage` is computed from one extra requested
item. Reviewer votes and approvals are not part of v1.

## Merge methods

Azure DevOps completion is a separate operation from reviewer votes. A reviewer
vote never completes a pull request, and completing a pull request does not cast
a vote.

| `MergeMethod` | Azure DevOps `mergeStrategy` |
| --- | --- |
| `Merge` | `noFastForward` |
| `Squash` | `squash` |
| `Rebase` | `rebase` |

`MergePullRequestAsync` reads the pull request first and sends its
`lastMergeSourceCommit` with the completion. This prevents completing a source
branch the caller did not review. The provider does not retry a failed merge.

- `DeleteSourceBranch` maps to `completionOptions.deleteSourceBranch`.
- `CommitTitle` and `CommitMessage` map to the merge commit message.
- Branch policies, required reviewers, and build validation can block
  completion.
- A conflict, a stale source commit, or an invalid transition throws
  `VcsConflictException`. A pull request that is already completed or abandoned
  also throws `VcsConflictException`.
- A missing pull request throws `PullRequestNotFoundException`.

`ClosePullRequestAsync` never merges. It sets `status = abandoned`. It reads the
pull request first and skips the write when the pull request is already
abandoned or completed, so close is idempotent. A completed pull request is
returned in `PullRequestState.Merged`.

## Error mapping

| Azure DevOps response | VcsManager exception |
| --- | --- |
| 401 Unauthorized | `VcsAuthenticationException` |
| 403 Forbidden | `VcsAuthorizationException` |
| 404 Not Found | `RepositoryNotFoundException` or `PullRequestNotFoundException` |
| 400 Bad Request on completion | `VcsConflictException` |
| 409 Conflict | `VcsConflictException` |
| 429 Too Many Requests | `VcsRateLimitException` with `RetryAfter` when the provider supplies it |
| 5xx | `VcsProviderException` |

## Code sample

This is the same flow used by the sample console app. It assumes the team
project and the branches `sample/merge` and `sample/close` already exist.

```csharp
using VcsManager;
using VcsManager.Builder;
using VcsManager.AzureDevOps;

var organizationUrl = Environment.GetEnvironmentVariable("VCS_AZUREDEVOPS_ORGANIZATION_URL");
var project = Environment.GetEnvironmentVariable("VCS_AZUREDEVOPS_DEFAULT_PROJECT");

var provider = new VcsProviderBuilder()
    .WithAzureDevOps(options =>
    {
        options.OrganizationUrl = organizationUrl;
        options.PersonalAccessToken = Environment.GetEnvironmentVariable("VCS_AZUREDEVOPS_PERSONAL_ACCESS_TOKEN");
        options.DefaultProject = project;
    })
    .Build();

await using (provider)
{
    var repositoryName =
        $"vcsmanager-sample-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..48];
    var repository = new RepositoryRef
    {
        Name = repositoryName,
        Project = project,
    };

    var created = await provider.CreateRepositoryAsync(new CreateRepositoryRequest
    {
        Name = repository.Name,
        Project = repository.Project,
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

- Creating projects.
- Cross-organization listing. One organization is configured per provider.
- The repository archive flag.
- `InitializeWithReadme` on repository creation.
- Reviewer votes, required reviewers, auto-complete, and merge queues.
- Member management.
- Azure DevOps Server authentication beyond PAT.

## References

- Azure DevOps REST API 7.1: <https://learn.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-7.1>
- Azure DevOps repositories API: <https://learn.microsoft.com/en-us/rest/api/azure/devops/git/repositories?view=azure-devops-rest-7.1>
- Azure DevOps pull requests API: <https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests?view=azure-devops-rest-7.1>
- Azure DevOps pull request update API: <https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests/update?view=azure-devops-rest-7.1>
- Microsoft.TeamFoundationServer.Client 20.256.2: <https://www.nuget.org/packages/Microsoft.TeamFoundationServer.Client/20.256.2>
- Azure DevOps personal access tokens: <https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate>
- Azure DevOps .NET samples: <https://github.com/microsoft/azure-devops-dotnet-samples>
