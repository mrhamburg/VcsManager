---
id: intro
title: Introduction
sidebar_label: Introduction
sidebar_position: 1
slug: /intro
---

# Introduction

<span className="badge-pill badge-dotnet">.NET 10.0</span>

## What is VcsManager?

VcsManager is a .NET library that gives applications one contract for common
version control operations. It provides a core abstraction package and one
package per provider, so the same application code can work with GitHub,
GitLab, Azure DevOps, or a self-hosted Gitea instance.

The library covers the operations most automation needs:

- Create, get, list, check, and delete repositories.
- Create, get, and list pull requests.
- Merge a pull request with a chosen merge method.
- Close a pull request without merging it.
- Page large listings with one consistent result shape.
- Add retries, timeouts, concurrency limits, logging, and OpenTelemetry around
  any provider.

:::tip[Why one contract?]
Swapping a provider does not change your business logic. Provider-native types
and HTTP responses stay inside the provider package. Public methods use only
core records, enums, and exceptions.
:::

## Packages

| Package | Purpose |
| --- | --- |
| `VcsManager.Core` | The `IVcsProvider` contract, builder, configuration readers, layers, and shared types. |
| `VcsManager.GitHub` | GitHub.com and GitHub Enterprise Server provider. |
| `VcsManager.GitLab` | GitLab.com and self-managed GitLab provider. |
| `VcsManager.AzureDevOps` | Azure DevOps Services provider. |
| `VcsManager.Gitea` | Self-hosted Gitea provider. |

Install the core package and one provider:

```bash title="Terminal"
dotnet add package VcsManager.Core
dotnet add package VcsManager.GitHub
```

Provider packages depend on `VcsManager.Core`, so installing a provider also
brings in the core package.

## Quick example

```csharp title="Program.cs"
using VcsManager;
using VcsManager.Builder;
using VcsManager.GitHub;

await using var provider = new VcsProviderBuilder()
    .WithGitHub(options =>
    {
        options.Token = Environment.GetEnvironmentVariable("VCS_GITHUB_TOKEN");
        options.DefaultOwner = "my-org";
    })
    .Build();

var repository = new RepositoryRef
{
    Owner = "my-org",
    Name = "payments-api",
};

var pullRequest = await provider.CreatePullRequestAsync(
    repository,
    new CreatePullRequestRequest
    {
        Title = "Add health endpoint",
        SourceBranch = "feature/health",
        TargetBranch = "main",
    });

var merged = await provider.MergePullRequestAsync(
    repository,
    pullRequest.Number,
    new MergePullRequestRequest
    {
        Method = MergeMethod.Squash,
        DeleteSourceBranch = true,
    });

Console.WriteLine($"Merged #{merged.Number}: {merged.State}");
```

## Design principles

- **Portable addresses.** `RepositoryRef` is the only repository identity.
  `Name` is required; `Owner` and `Project` are optional and provider-specific.
- **PAT authentication in v1.** Callers supply tokens through configuration.
  VcsManager never reads or persists credentials on its own.
- **One page at a time.** Listing calls fetch exactly one page. The caller
  controls paging.
- **Provider errors are normalized.** Every provider failure derives from
  `VcsProviderException`; provider SDK exceptions never escape a package.
- **Cross-cutting behavior is a layer.** Retry, timeout, concurrency, logging,
  and telemetry wrap any provider without changing it.

## Next steps

- [Install the packages](getting-started/installation.md).
- [Run the quick start](getting-started/quick-start.md).
- [Choose a provider](providers/overview.md).
- [Read the provider capability matrix](providers/capability-matrix.md).
- [Browse the API reference](api-reference.md).
