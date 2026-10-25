---
id: merge-and-close
title: Merge and Close Semantics
sidebar_label: Merge and Close
sidebar_position: 4
---

# Merge and Close Semantics

Merge and close are separate operations. A close call never merges, and a merge
call never closes without merging.

## Merge methods

`MergeMethod` has three values:

| `MergeMethod` | GitHub | GitLab | Azure DevOps | Gitea |
| --- | --- | --- | --- | --- |
| `Merge` | `merge` | Merge commit when the project uses `merge` | `noFastForward` | `do = merge` when `allow_merge_commits` |
| `Squash` | `squash` | `squash = true`, subject to the project squash setting | `squash` | `do = squash` when `allow_squash_merge` |
| `Rebase` | `rebase` | Rejected with `VcsUnsupportedOperationException` | `rebase` | `do = rebase` when `allow_rebase` or `allow_rebase_explicit` |

The provider never changes repository settings. A method that the repository
does not allow fails with `VcsConflictException` or
`VcsUnsupportedOperationException`; it never falls back to another method.

### Provider conditions

- **GitHub.** The repository must allow the requested method. Branch
  protection, required checks, and merge queues can still block the merge.
  `DeleteSourceBranch` removes the source Git reference after a successful
  merge. A fork branch is not deleted.
- **GitLab.** The project `merge_method` and squash settings control the
  outcome. `Rebase` is not available in v1 because the provider cannot verify
  the project settings per call. Failed merges return `VcsConflictException`.
- **Azure DevOps.** Completion reads the pull request and sends its
  `lastMergeSourceCommit`. Branch policies, required reviewers, and build
  validation can block completion. Reviewer votes are separate from completion.
- **Gitea.** Repository settings `allow_merge_commits`, `allow_squash_merge`,
  `allow_rebase`, and `allow_rebase_explicit` control success. A disabled style,
  conflict, archived repository, or invalid transition returns
  `VcsConflictException`.

## Merge a pull request

```csharp
using VcsManager;

var merged = await provider.MergePullRequestAsync(
    repository,
    pullRequest.Number,
    new MergePullRequestRequest
    {
        Method = MergeMethod.Squash,
        DeleteSourceBranch = true,
        CommitTitle = "Add health endpoint",
        CommitMessage = "Squash the feature branch into one commit.",
    });
```

`MergePullRequestRequest` properties:

| Property | Default | Description |
| --- | --- | --- |
| `Method` | `MergeMethod.Merge` | Merge, squash, or rebase. |
| `DeleteSourceBranch` | `false` | Delete the source branch after a successful merge where supported. |
| `CommitTitle` | `null` | Merge or squash commit title. |
| `CommitMessage` | `null` | Merge or squash commit message. |

Merge is not automatically retried. A failed merge can have a side effect on
the provider, so retrying it must be a caller decision.

## Close a pull request

`ClosePullRequestAsync` transitions an open or draft pull request to closed
without merging it.

| Current state | Result |
| --- | --- |
| Open or draft | Becomes closed; the updated pull request is returned. |
| Closed | No-op; the current pull request is returned. |
| Merged or completed | No-op; the current pull request is returned unchanged. |

Close is idempotent. It does not throw because the pull request is already
closed or merged, and it never merges as a side effect.

```csharp
var closed = await provider.ClosePullRequestAsync(repository, pullRequest.Number);

if (closed.State == PullRequestState.Merged)
{
    Console.WriteLine("The pull request was already merged.");
}
```

## State mapping

`PullRequestState` values:

| Value | Meaning |
| --- | --- |
| `Open` | Open for review. |
| `Draft` | Open but marked as a draft. |
| `Merged` | Merged or completed. |
| `Closed` | Closed without merging. |

A merged pull request reports `Merged` even when the provider uses a `closed`
state internally. Use `MergedAt` to observe the merge time.

## Related reading

- [Provider capability matrix](../providers/capability-matrix.md)
- [GitHub provider](../providers/github.md)
- [GitLab provider](../providers/gitlab.md)
- [Azure DevOps provider](../providers/azure-devops.md)
- [Gitea provider](../providers/gitea.md)
- [Pagination and errors](pagination-and-errors.md)
