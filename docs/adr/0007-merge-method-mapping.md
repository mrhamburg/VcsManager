# ADR-0007: Merge method mapping

- Status: Accepted
- Date: 2026-10-25

## Context

`MergeMethod` has three values: `Merge`, `Squash`, and `Rebase`. The providers
do not use the same vocabulary, and some methods depend on repository or project
settings that VcsManager does not control. A consistent mapping must state both
the request value and the conditions under which it succeeds.

## Decision

`MergeMethod` maps as follows:

| `MergeMethod` | GitHub `merge_method` | GitLab action | Azure DevOps `mergeStrategy` |
| --- | --- | --- | --- |
| `Merge` | `merge` | Merge with the project `merge_method` set to `merge` (merge commit) | `noFastForward` |
| `Squash` | `squash` | Merge with `squash = true` | `squash` |
| `Rebase` | `rebase` | Rebase the source branch, then merge under the project `merge_method` (`rebase_merge` or `ff`) | `rebase` |

Conditions and limits:

- **GitHub.** The repository must allow the requested method
  (`allow_merge_commit`, `allow_squash_merge`, `allow_rebase_merge`). Branch
  protection and required checks can still reject the merge. A rejected method
  maps to an unsupported-operation or validation error, not a silent fallback.
- **GitLab.** The project merge method is a project setting, not a per-request
  choice. `Merge` works when the project uses `merge`. `Squash` sets
  `squash = true`, but a project setting of `Require` or `Do not allow` takes
  precedence. `Rebase` works only when the project uses `rebase_merge` or `ff`
  and the rebase can complete without conflicts; with `merge`, GitLab still
  produces a merge commit. VcsManager never changes project settings.
- **Azure DevOps.** `Merge` maps to a two-parent no-fast-forward commit
  (`noFastForward`), not to `rebaseMerge`. `Rebase` maps to `rebase`, which
  updates the source branch and fast-forwards the target. Branch policies,
  required reviewers, and build validation can still block completion.
- Azure DevOps reviewer votes are separate from completion (ADR-0005). An
  approved vote does not imply a merge, and a merge request does not cast a
  vote.
- Azure DevOps `rebaseMerge` is not exposed in v1 because `MergeMethod` has no
  distinct value for it. Adding it would be a new `MergeMethod` value and a new
  ADR.
- `DeleteSourceBranch`, `CommitTitle`, and `CommitMessage` are passed through
  where the provider supports them. Unsupported fields are ignored only if the
  provider contract documents that behavior; otherwise they cause a validation
  error.

## Consequences

- Callers can request a method portably, but success depends on provider
  settings that the library cannot override.
- Provider tests must cover each method and the provider-specific rejection
  cases.
- The mapping is stable for v1; a provider-specific method requires a new
  enum value and a new ADR.

## References

- GitHub merge a pull request: <https://docs.github.com/en/rest/pulls/pulls#merge-a-pull-request>
- GitLab merge methods: <https://docs.gitlab.com/user/project/merge_requests/methods/>
- GitLab merge a merge request: <https://docs.gitlab.com/api/merge_requests/#merge-a-merge-request>
- Azure DevOps pull request update: <https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests/update?view=azure-devops-rest-7.1>
