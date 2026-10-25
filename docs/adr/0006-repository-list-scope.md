# ADR-0006: Repository listing scope

- Status: Accepted
- Date: 2026-10-25

## Context

Each provider defines repository visibility differently. Listing "all
repositories" is not a single portable operation: the result depends on the
token's permissions, the organization or namespace, and whether archived
repositories are included.

## Decision

`ListRepositoriesAsync` returns **repositories visible to the supplied token
within the configured owner, namespace, or project scope**.

| `RepositoryQuery` input | GitHub | GitLab | Azure DevOps |
| --- | --- | --- | --- |
| `Owner` set | User or organization repositories for that owner | Projects in that group or user namespace | Organization repositories |
| `Owner` and `Project` set | Validation error (`Project` is not used) | Projects in the subgroup path | Repositories in the team project |
| `Owner` not set | Repositories visible to the authenticated user | Projects visible to the authenticated user | Organization from configuration, or a configuration error if no organization is set |

Rules:

- Results are always limited by token permissions. VcsManager never widens the
  query or requests additional scopes.
- `IncludeArchived` defaults to `false`. GitHub filters on the `archived` field,
  GitLab uses the project `archived` query parameter, and Azure DevOps has no
  repository archive concept, so the flag has no effect there.
- `Visibility` and `NameContains` filters are applied provider-side when the
  provider supports them. Otherwise the provider filters the current page after
  retrieval and documents the difference. Pagination metadata still describes
  the provider response, not the filtered subset.
- Group and organization listings include nested GitLab subgroups only when the
  caller sets `Project`; Azure DevOps does not cross team projects unless the
  organization scope is requested.
- A listing never crosses organizations or provider hosts.
- Results are paginated according to ADR-0009. The provider does not fetch all
  pages to satisfy a single request.

## Consequences

- Callers get predictable, permission-bounded results on every provider.
- "List everything" is not portable; callers must supply the scope they need.
- Archived handling differs by provider and is documented in the capability
  matrix.
- Unit tests cover each scope branch and the archived filter.

## References

- GitHub repository endpoints: [https://docs.github.com/en/rest/repos/repos](https://docs.github.com/en/rest/repos/repos)
- GitLab projects API: [https://docs.gitlab.com/api/projects/](https://docs.gitlab.com/api/projects/)
- Azure DevOps repositories API: [https://learn.microsoft.com/en-us/rest/api/azure/devops/git/repositories?view=azure-devops-rest-7.1](https://learn.microsoft.com/en-us/rest/api/azure/devops/git/repositories?view=azure-devops-rest-7.1)
