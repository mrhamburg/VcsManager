# ADR-0001: Repository addressing

- Status: Accepted
- Date: 2026-10-25

## Context

Provider APIs identify repositories differently. GitHub uses `owner/name`,
GitLab uses a namespace path that can contain subgroups, and Azure DevOps uses
`organization/project/repository`. A single address type must cover all three
without leaking provider identifiers.

## Decision

`RepositoryRef` is the only repository identity type accepted by provider
methods.

| Property | Required | Meaning |
| --- | --- | --- |
| `Name` | Yes | Repository name, without any namespace path |
| `Owner` | No | User or organization (GitHub), top-level namespace (GitLab), organization (Azure DevOps) |
| `Project` | No | Subgroup path (GitLab) or team project (Azure DevOps); unused on GitHub |

Mapping to native addresses:

| Provider | Owner | Project | Name | Native address |
| --- | --- | --- | --- | --- |
| GitHub | User or organization | `null` | Repository | `{owner}/{name}` |
| GitLab | Top-level namespace | Optional subgroup path | Project | `{owner}/{project}/{name}`, or `{owner}/{name}` when `Project` is `null` |
| Azure DevOps | Organization | Team project | Repository | `{owner}/{project}/_git/{name}` |

Rules:

- A provider rejects a ref that omits a field its native address requires:
  `Owner` for GitHub and GitLab, and both `Owner` and `Project` for Azure
  DevOps.
- A provider rejects a field that has no meaning. For example, `Project` on
  GitHub is a validation error. Silently ignoring it hides caller mistakes.
- `Owner` and `Project` are single path segments on GitHub and Azure DevOps.
  GitLab `Project` can contain `/` for nested subgroups and is URL-encoded as
  one path.
- Mapping a native repository back to `VcsRepository` uses the same split, so a
  `RepositoryRef` round-trips through provider responses.

## Consequences

- Application code stays portable across providers for the three-part address
  shape.
- Non-portable shapes, such as a personal GitHub repository addressed with a
  project, fail fast with a validation error instead of producing a wrong URL.
- Each provider package owns its URL and path construction; no provider path
  syntax appears in the core public API.
- Provider unit tests must cover required-field and unsupported-field
  validation.

## References

- GitHub repository endpoints: <https://docs.github.com/en/rest/repos/repos>
- GitLab projects API: <https://docs.gitlab.com/api/projects/>
- Azure DevOps repositories API: <https://learn.microsoft.com/en-us/rest/api/azure/devops/git/repositories?view=azure-devops-rest-7.1>
