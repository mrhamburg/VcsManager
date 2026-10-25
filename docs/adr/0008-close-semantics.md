# ADR-0008: Close semantics and idempotency

- Status: Accepted
- Date: 2026-10-25

## Context

Closing a pull request does not mean merging it. Providers also treat terminal
states differently: a merged or completed pull request cannot normally be
closed again, and some APIs return an error when the requested transition is
not valid. Callers need one predictable rule.

## Decision

`ClosePullRequestAsync` means **transition an open or draft pull request to
closed without merging it**.

| Current state | Result |
| --- | --- |
| Open or draft | Pull request becomes closed; the updated pull request is returned |
| Closed | No-op; the current pull request is returned |
| Merged or completed | No-op; the current pull request is returned unchanged |

Close is therefore idempotent. It never throws because the pull request is
already closed or already merged.

Mapping:

| Provider | Close operation | No-op handling |
| --- | --- | --- |
| GitHub | `PATCH /repos/{owner}/{repo}/pulls/{number}` with `state = closed` | Read state first; skip the write when the pull request is closed or merged |
| GitLab | `PUT /projects/:id/merge_requests/:iid` with `state_event = close` | Read state first; skip the write when the merge request is closed or merged |
| Azure DevOps | `PATCH .../pullrequests/{id}` with `status = abandoned` | Read status first; skip the write when the pull request is abandoned or completed |

Additional rules:

- A missing repository or pull request maps to the core not-found error.
- A permission failure maps to the core authentication or authorization error.
- Closing does not delete the source branch and does not change the target
  branch.
- Merging is a separate operation (ADR-0007). A close call never merges as a
  side effect.
- The provider returns the pull request as it exists after the operation, so
  callers can observe the final state without a second request.

## Consequences

- Repeated close calls are safe for retry logic and idempotent jobs.
- The read-before-write adds one request for close. This is the cost of
  consistent no-op behavior across providers.
- Tests cover open, closed, and merged/completed states for each provider.

## References

- GitHub update a pull request: [https://docs.github.com/en/rest/pulls/pulls#update-a-pull-request](https://docs.github.com/en/rest/pulls/pulls#update-a-pull-request)
- GitLab update a merge request: [https://docs.gitlab.com/api/merge_requests/#update-a-merge-request](https://docs.gitlab.com/api/merge_requests/#update-a-merge-request)
- Azure DevOps pull request update: [https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests/update?view=azure-devops-rest-7.1](https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests/update?view=azure-devops-rest-7.1)
