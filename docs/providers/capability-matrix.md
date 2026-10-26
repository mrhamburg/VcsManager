# Provider capability matrix

This matrix describes VcsManager v1 behavior. It is a companion to the
[architecture decision records](../adr/README.md), especially
[ADR-0007](../adr/0007-merge-method-mapping.md) and
[ADR-0009](../adr/0009-pagination-and-errors.md).

## Legend

| Value | Meaning |
| --- | --- |
| **Supported** | Available for resources the token can access, with no provider-specific configuration beyond permissions. |
| **Conditional** | Available, but provider settings, permissions, policies, or resource state can prevent it. The notes state the condition. |
| **Unsupported** | No v1 mapping. |

## Capability summary

| Capability | GitHub | GitLab | Azure DevOps | Gitea |
| --- | --- | --- | --- | --- |
| Create repository | Supported | Supported | Conditional | Supported |
| Seed repository | Supported | Supported | Supported | Supported |
| Delete repository | Supported | Supported | Supported | Supported |
| List repositories | Supported | Supported | Supported | Supported |
| Create pull request | Supported | Supported | Supported | Supported |
| Merge pull request | Conditional | Conditional | Conditional | Conditional |
| Close pull request | Supported | Supported | Supported | Supported |

## Repository addressing

| Provider | `Owner` | `Project` | `Name` | Native address |
| --- | --- | --- | --- | --- |
| GitHub | User or organization | Unsupported | Repository | `{owner}/{name}` |
| GitLab | Top-level namespace | Optional subgroup path | Project | `{owner}/{project}/{name}` or `{owner}/{name}` |
| Azure DevOps | Organization (defaults to `OrganizationUrl`) | Team project (required unless `DefaultProject` is set) | Repository | `{owner}/{project}/_git/{name}` |
| Gitea | User or organization (defaults to `DefaultOwner`) | Unsupported | Repository | `{owner}/{name}` |

See [ADR-0001](../adr/0001-repository-addressing.md) for validation rules.

## Capability details

### Create repository

| Provider | Status | Notes |
| --- | --- | --- |
| GitHub | Supported | `Owner` selects an organization; when absent, `DefaultOwner` or the authenticated user is used. `Project` is unsupported. Public, private, and (GitHub Enterprise only) internal visibility are mapped. `InitializeWithReadme` creates an initial commit. `DefaultBranch` is applied after creation when the repository already has a branch. Use `SeedRepositoryAsync` with `InitializeWithReadme = false` for template content. |
| GitLab | Supported | `Owner` is required. Nested subgroups use `Project`. Public, internal, and private visibility are mapped. `InitializeWithReadme` creates an initial commit. Use `SeedRepositoryAsync` with `InitializeWithReadme = false` for template content. |
| Azure DevOps | Conditional | The team project must already exist and is resolved from `Project` or `DefaultProject`. The organization defaults to `OrganizationUrl`. The repository is created empty; `InitializeWithReadme` is unsupported and returns a validation error, and `DefaultBranch` is ignored. Use `SeedRepositoryAsync` to create the first commit. The token needs project administration rights for policies that require them. |
| Gitea | Supported | `Owner` is a user or organization and defaults to `DefaultOwner`. Organization repositories use the organization endpoint; user repositories are created under the token account. Public and private visibility are mapped; `Internal` is rejected. `InitializeWithReadme` creates an initial commit. Use `SeedRepositoryAsync` with `InitializeWithReadme = false` for template content. `Project` is unsupported. |

### Seed repository

| Provider | Status | Notes |
| --- | --- | --- |
| GitHub | Supported | Writes one initial commit with all files. GitHub rejects the Git data API on a repository without commits, so the branch is created with the first file through the Contents API and its tip is then replaced by a single root commit that contains every file. The repository must be empty. The seeded branch becomes the default branch. The token needs contents write access. |
| GitLab | Supported | Writes one commit with all files through the Commits API. The project must be empty. The seeded branch becomes the default branch. The token needs `api` or `write_repository`. |
| Azure DevOps | Supported | Writes one initial commit through the Pushes API. The team project must exist and the repository must be empty. The first branch becomes the default branch. The token needs Code (Read & Write). |
| Gitea | Supported | Writes one commit with all files through the multi-file contents API. The repository must be empty. Gitea sets the default branch on the first commit. The token needs repository write access. |

Seeding creates exactly one commit. It does not chunk large templates and does
not preserve executable bits, symlinks, submodules, or empty directories. See
[ADR-0013](../adr/0013-repository-seeding.md).

### Delete repository

| Provider | Status | Notes |
| --- | --- | --- |
| GitHub | Supported | Requires repository administration and the `delete_repo` scope. |
| GitLab | Supported | Requires the Owner or Maintainer role according to project settings. A project marked for deletion is reported as not found. |
| Azure DevOps | Supported | Requires project administration. Deletion may be disabled by organization policy. |
| Gitea | Supported | Requires repository write access. |

Deleting a repository is not idempotent across providers: a second delete
usually returns not found. Callers handle the not-found error explicitly.

### List repositories

| Provider | Status | Notes |
| --- | --- | --- |
| GitHub | Supported | Lists repositories for `Owner` (organization) or all repositories visible to the token when `Owner` is absent. Archived repositories are included only when requested. |
| GitLab | Supported | Lists projects in `Owner` (group), optionally in the subgroup `Project`, or all projects visible to the token. `IncludeArchived` maps to the project `archived` filter. |
| Azure DevOps | Conditional | Lists one team project from `Project` or `DefaultProject`; without either, it uses the organization-level REST listing for the configured organization. `NameContains` and `Visibility` apply to the returned page. Azure DevOps has no repository archive concept, so `IncludeArchived` has no effect and repositories report `Private`. `TotalCount` is `null`. |
| Gitea | Supported | Organization owners use `/orgs/{owner}/repos`; user owners resolve the user ID and use `/repos/search?uid={id}`; no owner uses `/repos/search`. Archived repositories are excluded unless requested. `NameContains` maps to `q` where supported. `Internal` visibility is rejected. The page size is capped at 50. |

All listings are bounded by token permissions. See
[ADR-0006](../adr/0006-repository-list-scope.md).

### Create pull request

| Provider | Status | Notes |
| --- | --- | --- |
| GitHub | Supported | Source and target branches must exist. Draft pull requests are supported. Cross-repository pull requests require a fork and are not a v1 guarantee. |
| GitLab | Supported | Source and target branches must exist in the project. Draft is supported. Project merge request settings can still block later merge. |
| Azure DevOps | Supported | The target repository and project must exist. Draft pull requests are supported. Branch policies do not block creation, only completion. |
| Gitea | Supported | Source and target branches must exist. Draft pull requests use the `WIP:` title convention. Repository merge settings do not block creation. |

### Merge pull request

| Provider | Status | Notes |
| --- | --- | --- |
| GitHub | Conditional | The repository must allow the requested `merge`, `squash`, or `rebase` method. Branch protection, required checks, and merge queue rules can reject the merge. |
| GitLab | Conditional | The project `merge_method` and squash settings control the outcome of `Merge` and `Squash`. `Rebase` is rejected in v1 because the provider cannot verify the project settings per call. Conflicts and failed pipelines can block the merge. |
| Azure DevOps | Conditional | Completion reads the pull request, sends `lastMergeSourceCommit`, and uses `noFastForward`, `squash`, or `rebase`. Branch policies, required reviewers, and build validation can block completion. Reviewer votes are separate from completion and never merge a pull request. |
| Gitea | Conditional | `Merge`, `Squash`, and `Rebase` map to `do = merge`, `squash`, and `rebase`. Repository settings `allow_merge_commits`, `allow_squash_merge`, `allow_rebase`, and `allow_rebase_explicit` control success. A disabled style, conflict, archived repository, or invalid transition returns `VcsConflictException`. Reviews are not part of v1. |

### Close pull request

| Provider | Status | Notes |
| --- | --- | --- |
| GitHub | Supported | Idempotent. Closing an open pull request sets `state = closed`. A closed or merged pull request is returned unchanged. |
| GitLab | Supported | Idempotent. Closing uses `state_event = close`. A closed or merged merge request is returned unchanged. |
| Azure DevOps | Supported | Idempotent. Closing sets `status = abandoned`. An abandoned or completed pull request is returned unchanged. |
| Gitea | Supported | Idempotent. Closing sets `state = closed`. A merged (`state = closed`, `merged = true`) or closed pull request is returned unchanged and merged stays `Merged`. |

See [ADR-0008](../adr/0008-close-semantics.md).

## Merge method support

| `MergeMethod` | GitHub | GitLab | Azure DevOps | Gitea |
| --- | --- | --- | --- | --- |
| `Merge` | Supported: `merge` | Conditional: merge commit when project `merge_method = merge` | Supported: `noFastForward` | Conditional: `do = merge` when `allow_merge_commits` |
| `Squash` | Supported: `squash` | Conditional: `squash = true`, subject to the project squash setting | Supported: `squash` | Conditional: `do = squash` when `allow_squash_merge` |
| `Rebase` | Supported: `rebase` | Unsupported in v1: rejected with `VcsUnsupportedOperationException` | Supported: `rebase` | Conditional: `do = rebase` when `allow_rebase` or `allow_rebase_explicit` |

## Not supported in v1

| Provider | Not supported |
| --- | --- |
| GitHub | Project-scoped repository addressing; OAuth and GitHub App authentication; provider-reported total counts for pagination |
| GitLab | GraphQL-only operations; changing project merge settings on the caller's behalf; the `Rebase` merge method |
| Azure DevOps | Repository archive flag; cross-organization listing; reviewer vote management as part of merge; `rebaseMerge` as a distinct method; `InitializeWithReadme` on repository creation. Use `SeedRepositoryAsync` for the first commit. |
| Gitea | Project-scoped addressing; internal repository visibility; approvals and review requests; member management; `rebase-merge`, `fast-forward-only`, and `manually-merged` merge styles |

## Provider documentation

- [GitHub provider configuration and behavior](github.md)
- [GitLab provider configuration and behavior](gitlab.md)
- [Azure DevOps provider configuration and behavior](azure-devops.md)
- [Gitea provider configuration and behavior](gitea.md)
- GitHub REST API for repositories: [https://docs.github.com/en/rest/repos/repos](https://docs.github.com/en/rest/repos/repos)
- GitHub REST API for pull requests: [https://docs.github.com/en/rest/pulls/pulls](https://docs.github.com/en/rest/pulls/pulls)
- GitLab REST API: [https://docs.gitlab.com/api/rest/](https://docs.gitlab.com/api/rest/)
- GitLab projects API: [https://docs.gitlab.com/api/projects/](https://docs.gitlab.com/api/projects/)
- GitLab merge requests API: [https://docs.gitlab.com/api/merge_requests/](https://docs.gitlab.com/api/merge_requests/)
- GitLab merge methods: [https://docs.gitlab.com/user/project/merge_requests/methods/](https://docs.gitlab.com/user/project/merge_requests/methods/)
- Azure DevOps REST API 7.1: [https://learn.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-7.1](https://learn.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-7.1)
- Azure DevOps repositories API: [https://learn.microsoft.com/en-us/rest/api/azure/devops/git/repositories?view=azure-devops-rest-7.1](https://learn.microsoft.com/en-us/rest/api/azure/devops/git/repositories?view=azure-devops-rest-7.1)
- Azure DevOps pull requests API: [https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests?view=azure-devops-rest-7.1](https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests?view=azure-devops-rest-7.1)
- Gitea API documentation: [https://docs.gitea.com/api/1.0/](https://docs.gitea.com/api/1.0/)
- Gitea repository API: [https://docs.gitea.com/api/1.0/#tag/repository](https://docs.gitea.com/api/1.0/#tag/repository)
- Gitea pull request API: [https://docs.gitea.com/api/1.0/#tag/pull-request](https://docs.gitea.com/api/1.0/#tag/pull-request)
