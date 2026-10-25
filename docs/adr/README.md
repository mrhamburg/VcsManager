---
id: adr-index
title: Architecture decision records
sidebar_label: Overview
sidebar_position: 0
---

# Architecture decision records

These short records capture decisions that later VcsManager issues depend on.
An accepted ADR is not rewritten; a changed decision gets a new ADR that
supersedes it.

| ADR | Decision | Status |
| --- | --- | --- |
| [0001](0001-repository-addressing.md) | Repository addressing with `RepositoryRef` | Accepted |
| [0002](0002-authentication.md) | PAT authentication for v1 | Accepted |
| [0003](0003-github-client.md) | GitHub uses Octokit 14.0.0 behind an internal client | Accepted |
| [0004](0004-gitlab-client.md) | GitLab uses REST API v4 with `HttpClient` | Accepted |
| [0005](0005-azure-devops-client.md) | Azure DevOps uses the official SDK with REST 7.1 fallback | Accepted |
| [0006](0006-repository-list-scope.md) | Repository listing scope | Accepted |
| [0007](0007-merge-method-mapping.md) | Merge method mapping | Accepted |
| [0008](0008-close-semantics.md) | Close semantics and idempotency | Accepted |
| [0009](0009-pagination-and-errors.md) | Pagination and error mapping | Accepted |
| [0010](0010-exception-contract.md) | Exception contract and HTTP status mapping | Accepted |
| [0011](0011-gitea-client.md) | Gitea uses REST API v1 with `HttpClient` | Accepted |
| [0012](0012-provider-listing-and-merge-scope.md) | Provider listing and merge scope for v1 | Accepted |
| [0013](0013-repository-seeding.md) | Repository seeding from a template | Accepted |

See the [provider capability matrix](../providers/capability-matrix.md) for the
supported, conditional, and unsupported operations per provider.
