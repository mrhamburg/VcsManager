# ADR-0012: Provider listing and merge scope for v1

- Status: Accepted
- Date: 2026-10-25

## Context

ADR-0006 and ADR-0007 describe the intended portable behavior. The shipped v1
provider clients implement a narrower scope for two cases:

- GitHub and GitLab do not distinguish a user owner from an organization or
  group owner when a listing supplies `Owner`. The GitHub client uses the
  organization endpoint and the GitLab client uses the group endpoint.
- The GitLab client rejects `MergeMethod.Rebase` because it cannot verify the
  project merge method per call.

The provider pages and the capability matrix already describe the narrower
behavior. This record makes that behavior an explicit v1 decision so the ADRs
and the code do not disagree.

## Decision

- GitHub `ListRepositoriesAsync` with `Owner` lists organization repositories.
  Without `Owner`, it lists repositories visible to the authenticated user.
- GitLab `ListRepositoriesAsync` with `Owner` lists group projects. Without
  `Owner`, it lists projects visible to the authenticated user.
- GitLab `MergePullRequestAsync` rejects `MergeMethod.Rebase` with
  `VcsUnsupportedOperationException`.
- User-scoped owner listing and GitLab rebase support require a new ADR and a
  separate implementation change.

These decisions supersede the corresponding parts of ADR-0006 and ADR-0007.

## Consequences

- Callers that need a user-owned repository scope must list without `Owner` and
  filter the returned page, or address the repository directly.
- The capability matrix and provider pages describe the same scope as the code.
- A later change that adds user-scope detection or GitLab rebase support must
  supersede this ADR.

## References

- GitHub repository endpoints: [https://docs.github.com/en/rest/repos/repos](https://docs.github.com/en/rest/repos/repos)
- GitLab projects API: [https://docs.gitlab.com/api/projects/](https://docs.gitlab.com/api/projects/)
- GitLab merge methods: [https://docs.gitlab.com/user/project/merge_requests/methods/](https://docs.gitlab.com/user/project/merge_requests/methods/)
