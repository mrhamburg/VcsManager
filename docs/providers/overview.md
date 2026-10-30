---
id: overview
title: Providers Overview
sidebar_label: Overview
sidebar_position: 1
---

# Providers Overview

VcsManager ships one provider package for each supported system. All providers
implement `IVcsProvider` and return the same core records and exceptions.

## Available providers

| Provider | Package | Authentication | Native address |
| --- | --- | --- | --- |
| [GitHub](github.md) | `VcsManager.GitHub` | Personal access token | `{owner}/{name}` |
| [GitLab](gitlab.md) | `VcsManager.GitLab` | Personal access token | `{owner}/{project}/{name}` or `{owner}/{name}` |
| [Azure DevOps](azure-devops.md) | `VcsManager.AzureDevOps` | Personal access token | `{organization}/{project}/{name}` |
| [Gitea](gitea.md) | `VcsManager.Gitea` | Access token | `{owner}/{name}` |

## Integration test status

Integration tests run each provider against a live service. The table shows the
latest result on `main`. A grey `no check runs` pill means the provider's CI
secrets are not configured, so the job is skipped.

| Provider | Latest status on `main` |
| --- | --- |
| GitHub | [![GitHub integration tests](https://img.shields.io/github/check-runs/mrhamburg/VcsManager/main?nameFilter=Integration%20tests%20%28GitHub%29&label=github)](https://github.com/mrhamburg/VcsManager/actions/workflows/ci.yml) |
| GitLab | [![GitLab integration tests](https://img.shields.io/github/check-runs/mrhamburg/VcsManager/main?nameFilter=Integration%20tests%20%28GitLab%29&label=gitlab)](https://github.com/mrhamburg/VcsManager/actions/workflows/ci.yml) |
| Azure DevOps | [![Azure DevOps integration tests](https://img.shields.io/github/check-runs/mrhamburg/VcsManager/main?nameFilter=Integration%20tests%20%28Azure%20DevOps%29&label=azure%20devops)](https://github.com/mrhamburg/VcsManager/actions/workflows/ci.yml) |
| Gitea | [![Gitea integration tests](https://img.shields.io/github/check-runs/mrhamburg/VcsManager/main?nameFilter=Integration%20tests%20%28Gitea%29&label=gitea)](https://github.com/mrhamburg/VcsManager/actions/workflows/ci.yml) |

## Capability summary

| Capability | GitHub | GitLab | Azure DevOps | Gitea |
| --- | :---: | :---: | :---: | :---: |
| Create repository | Yes | Yes | Conditional | Yes |
| Delete repository | Yes | Yes | Yes | Yes |
| List repositories | Yes | Yes | Conditional | Yes |
| Create pull request | Yes | Yes | Yes | Yes |
| Merge pull request | Conditional | Conditional | Conditional | Conditional |
| Close pull request | Yes | Yes | Yes | Yes |

"Conditional" means the operation is available, but repository settings,
branch policies, required checks, or resource state can prevent it. The
[provider capability matrix](capability-matrix.md) gives the exact conditions.

## Register a provider

Each provider has a fluent extension method:

```csharp
using VcsManager.Builder;

var builder = new VcsProviderBuilder();

builder.WithGitHub();       // provider name "github"
builder.WithGitLab();       // provider name "gitlab"
builder.WithAzureDevOps();  // provider name "azuredevops"
builder.WithGitea();        // provider name "gitea"
```

The provider name is used by `VCS_PROVIDER` and by the YAML provider block.
Matching is case-insensitive.

## Repository addressing

`RepositoryRef` is the only repository identity:

```csharp
var repository = new RepositoryRef
{
    Owner = "my-org",
    Project = "my-project",
    Name = "payments-api",
};
```

| Provider | `Owner` | `Project` | `Name` |
| --- | --- | --- | --- |
| GitHub | User or organization | Unsupported | Repository |
| GitLab | Top-level namespace | Optional subgroup path | Project path |
| Azure DevOps | Organization, defaulting to `OrganizationUrl` | Team project, defaulting to `DefaultProject` | Repository |
| Gitea | User or organization, defaulting to `DefaultOwner` | Unsupported | Repository |

A provider rejects a ref that omits a required field and a ref that sets a field
the provider cannot express. For example, GitHub rejects `Project` instead of
ignoring it.

## Choose a provider

### GitHub

Use GitHub when your repositories live on GitHub.com or GitHub Enterprise
Server. The provider uses Octokit behind an internal client. It supports
classic and fine-grained personal access tokens.

### GitLab

Use GitLab when your projects live on GitLab.com or a self-managed GitLab.
GitLab merge requests are returned as `PullRequest`; the project IID is
`PullRequest.Number`. The provider uses REST API v4.

### Azure DevOps

Use Azure DevOps when your repositories live in Azure DevOps Services. The
provider uses the official `Microsoft.TeamFoundationServer.Client` SDK with REST
API 7.1 as a fallback. A team project is required for most repository
operations.

### Gitea

Use Gitea when you run a self-hosted Gitea instance. The base URL is required.
Gitea has no project or subgroup level, and it has no internal repository
visibility. The page size is capped at 50.

## Provider behavior

The provider pages document configuration keys, token scopes, addressing,
listing scope, merge methods, close behavior, and error mapping:

- [GitHub](github.md)
- [GitLab](gitlab.md)
- [Azure DevOps](azure-devops.md)
- [Gitea](gitea.md)
- [Capability matrix](capability-matrix.md)
