---
id: installation
title: Installation
sidebar_label: Installation
sidebar_position: 1
---

# Installation

VcsManager uses a modular layout. Install the core package and the provider
packages your application needs.

## Requirements

- .NET 10 SDK or later.
- A GitHub, GitLab, Azure DevOps, or Gitea account for real provider calls.
- A personal access token (PAT) with the permissions listed on the provider
  page.

## Core package

The core package contains `IVcsProvider`, the builder, configuration readers,
shared records, exceptions, and operational layers:

```bash title="Terminal"
dotnet add package VcsManager.Core
```

The package ID is `VcsManager.Core`.

## Provider packages

Choose one or more providers:

```bash title="Terminal"
dotnet add package VcsManager.GitHub
dotnet add package VcsManager.GitLab
dotnet add package VcsManager.AzureDevOps
dotnet add package VcsManager.Gitea
```

| Provider | Package | Register and configure |
| --- | --- | --- |
| GitHub | `VcsManager.GitHub` | `WithGitHub()` |
| GitLab | `VcsManager.GitLab` | `WithGitLab()` |
| Azure DevOps | `VcsManager.AzureDevOps` | `WithAzureDevOps()` |
| Gitea | `VcsManager.Gitea` | `WithGitea()` |

Provider packages depend on `VcsManager.Core`. Installing a provider package
also installs the core package when it is not already present.

## Verify the installation

A provider builder needs a registration call and a configuration source. This
snippet compiles without making a network call:

```csharp title="Program.cs"
using VcsManager;
using VcsManager.Builder;
using VcsManager.GitHub;

var builder = new VcsProviderBuilder()
    .WithGitHub(options =>
    {
        options.Token = "replace-with-a-real-token";
        options.DefaultOwner = "my-org";
    });

IVcsProvider provider = builder.Build();
```

Dispose the provider when the application no longer needs it:

```csharp
await provider.DisposeAsync();
```

## Build from source

```bash
dotnet restore
dotnet build
dotnet test
```

## Sample application

The sample console application runs the full repository and pull request flow
with the built-in in-memory `demo` provider:

```bash
cd samples/VcsManager.Samples
VCS_PROVIDER=demo dotnet run
```

For a real provider, use the provider documentation and its configuration
keys. The sample flow is the same for every provider.

## Next steps

- [Quick start](quick-start.md).
- [Configuration](configuration.md).
- [Provider overview](../providers/overview.md).
