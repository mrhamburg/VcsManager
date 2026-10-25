---
id: api-reference
title: API Reference
sidebar_label: API Reference
sidebar_position: 5
---

# API Reference

This page summarizes the public surface. All public types and members carry XML
documentation in the package.

## Namespaces

| Namespace | Contents |
| --- | --- |
| `VcsManager` | `IVcsProvider`, shared records, enums, exceptions, and layers. |
| `VcsManager.Builder` | `VcsProviderBuilder`, configuration readers, factories, and layer extensions. |
| `VcsManager.GitHub` | `GitHubOptions` and `WithGitHub()`. |
| `VcsManager.GitLab` | `GitLabOptions` and `WithGitLab()`. |
| `VcsManager.AzureDevOps` | `AzureDevOpsOptions` and `WithAzureDevOps()`. |
| `VcsManager.Gitea` | `GiteaOptions` and `WithGitea()`. |

## IVcsProvider

```csharp
public interface IVcsProvider : IAsyncDisposable
{
    Task<VcsRepository> CreateRepositoryAsync(
        CreateRepositoryRequest request,
        CancellationToken cancellationToken = default);

    Task SeedRepositoryAsync(
        RepositoryRef repository,
        SeedRepositoryRequest request,
        CancellationToken cancellationToken = default);

    Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default);

    Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default);

    Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default);

    Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default);

    Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default);

    Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default);
}
```

## Repository types

### RepositoryRef

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `Name` | `string` | Yes | Repository name. |
| `Owner` | `string?` | No | Owner, organization, or top-level namespace. |
| `Project` | `string?` | No | Project or subgroup. |

### CreateRepositoryRequest

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Name` | `string` | Required | Repository name. |
| `Owner` | `string?` | `null` | Owner, organization, or namespace. |
| `Project` | `string?` | `null` | Project or subgroup. |
| `Description` | `string?` | `null` | Repository description. |
| `Visibility` | `VcsVisibility` | `Private` | Private, internal, or public. |
| `InitializeWithReadme` | `bool` | `false` | Create an initial commit with a readme. |
| `DefaultBranch` | `string?` | `null` | Default branch when the provider supports it. |

### SeedRepositoryRequest

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `Branch` | `string` | Yes | Branch that receives the initial commit and becomes the default branch. |
| `Files` | `IReadOnlyList<RepositorySeedFile>` | Yes | Files to write in one initial commit. |
| `CommitMessage` | `string?` | No | Commit message. Defaults to `Initial commit`. |

### RepositorySeedFile

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `Path` | `string` | Yes | Relative path with forward slashes. |
| `Content` | `ReadOnlyMemory<byte>` | Yes | Binary file content. |

### RepositoryQuery

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Owner` | `string?` | `null` | Owner, organization, or namespace to list. |
| `Project` | `string?` | `null` | Project or subgroup to list. |
| `Visibility` | `VcsVisibility?` | `null` | Visibility to include. |
| `NameContains` | `string?` | `null` | Substring the repository name must contain. |
| `IncludeArchived` | `bool` | `false` | Include archived repositories. |

### VcsRepository

| Property | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Provider-specific identifier. |
| `Name` | `string` | Repository name. |
| `Owner` | `string?` | Owner, organization, or namespace. |
| `Project` | `string?` | Project or subgroup. |
| `FullName` | `string` | Full provider-specific name. |
| `Url` | `string` | Repository web URL. |
| `DefaultBranch` | `string?` | Default branch, when present. |
| `Visibility` | `VcsVisibility` | Repository visibility. |
| `Description` | `string?` | Repository description. |
| `IsArchived` | `bool` | Whether the repository is archived. |
| `CreatedAt` | `DateTimeOffset?` | Creation time. |
| `UpdatedAt` | `DateTimeOffset?` | Last update time. |

### VcsVisibility

`Private`, `Internal`, `Public`.

## Pull request types

### CreatePullRequestRequest

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `Title` | `string` | Yes | Pull request title. |
| `Description` | `string?` | No | Pull request description. |
| `SourceBranch` | `string` | Yes | Source branch. |
| `TargetBranch` | `string` | Yes | Target branch. |
| `Draft` | `bool` | No | Create as a draft. |

### PullRequestQuery

| Property | Type | Description |
| --- | --- | --- |
| `State` | `PullRequestState?` | State to include. |
| `Author` | `string?` | Author to include. |
| `SourceBranch` | `string?` | Source branch to include. |
| `TargetBranch` | `string?` | Target branch to include. |

### PullRequest

| Property | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Provider-specific identifier. |
| `Number` | `int` | Pull request number. |
| `Title` | `string` | Title. |
| `Description` | `string?` | Description. |
| `SourceBranch` | `string` | Source branch. |
| `TargetBranch` | `string` | Target branch. |
| `State` | `PullRequestState` | Open, draft, merged, or closed. |
| `IsDraft` | `bool` | Whether the pull request is a draft. |
| `Author` | `string` | Author. |
| `Url` | `string` | Web URL. |
| `CreatedAt` | `DateTimeOffset?` | Creation time. |
| `UpdatedAt` | `DateTimeOffset?` | Last update time. |
| `MergedAt` | `DateTimeOffset?` | Merge time. |
| `ClosedAt` | `DateTimeOffset?` | Close time. |

### PullRequestState

`Open`, `Draft`, `Merged`, `Closed`.

### MergePullRequestRequest

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Method` | `MergeMethod` | `Merge` | Merge, squash, or rebase. |
| `DeleteSourceBranch` | `bool` | `false` | Delete the source branch after a successful merge. |
| `CommitTitle` | `string?` | `null` | Commit title. |
| `CommitMessage` | `string?` | `null` | Commit message. |

### MergeMethod

`Merge`, `Squash`, `Rebase`.

## Pagination types

### PaginationOptions

| Member | Value | Description |
| --- | --- | --- |
| `DefaultPage` | `1` | Default page number. |
| `DefaultPageSize` | `30` | Default page size. |
| `MaxPageSize` | `100` | Maximum page size. |
| `Page` | `1` | Page number, at least 1. |
| `PageSize` | `30` | Page size, 1 to 100. |

### PagedResult&lt;T&gt;

| Property | Type | Description |
| --- | --- | --- |
| `Items` | `IReadOnlyList<T>` | Items on this page. |
| `Page` | `int` | Page number. |
| `PageSize` | `int` | Effective page size. |
| `TotalCount` | `int?` | Total count, or `null` when not reported. |
| `HasNextPage` | `bool` | Whether another page is available. |

## Builder

### VcsProviderBuilder

| Member | Description |
| --- | --- |
| `RegisterProvider(IVcsProviderFactory factory)` | Registers a factory by `ProviderName`. |
| `UseProvider(string providerName, Action<Dictionary<string, string>>? configure = null)` | Selects a provider and optional settings. |
| `FromEnvironment(string prefix = "VCS")` | Loads configuration from environment variables. |
| `FromYaml(string yaml)` | Loads configuration from a YAML string. |
| `FromYamlFile(string path)` | Loads configuration from a YAML file. |
| `AddLayer(IVcsProviderLayer layer)` | Adds a decorator. The first layer added is outermost. |
| `Build()` | Creates the provider and wraps it with the registered layers. |

### IVcsProviderFactory

| Member | Description |
| --- | --- |
| `ProviderName` | Name used in configuration and `UseProvider`. |
| `Create(IReadOnlyDictionary<string, string> configuration)` | Creates a provider from flat settings. |

### Configuration readers

| Type | Method |
| --- | --- |
| `EnvironmentVariableConfigurationReader` | `Read(string prefix = "VCS")` |
| `YamlConfigurationReader` | `Read(string yaml)` |

Both return `VcsProviderConfiguration` with `ProviderName` and `Settings`.

## Provider registration

| Extension method | Options type | Provider name |
| --- | --- | --- |
| `WithGitHub(Action<GitHubOptions>? configure = null)` | `GitHubOptions` | `github` |
| `WithGitLab(Action<GitLabOptions>? configure = null)` | `GitLabOptions` | `gitlab` |
| `WithAzureDevOps(Action<AzureDevOpsOptions>? configure = null)` | `AzureDevOpsOptions` | `azuredevops` |
| `WithGitea(Action<GiteaOptions>? configure = null)` | `GiteaOptions` | `gitea` |

### Option properties

| Options type | Properties |
| --- | --- |
| `GitHubOptions` | `Token`, `BaseUrl`, `DefaultOwner`, `RequestTimeout` |
| `GitLabOptions` | `Token`, `BaseUrl`, `DefaultNamespace` |
| `AzureDevOpsOptions` | `OrganizationUrl`, `PersonalAccessToken`, `DefaultProject`, `ApiVersion` |
| `GiteaOptions` | `BaseUrl`, `Token`, `DefaultOwner` |

## Layers

| Extension method | Layer | Options |
| --- | --- | --- |
| `WithRetry` | `RetryLayer` | `RetryLayerOptions` |
| `WithTimeout` | `TimeoutLayer` | `TimeoutLayerOptions` |
| `WithConcurrentLimiter` | `ConcurrentLimiterLayer` | `ConcurrentLimiterLayerOptions` |
| `WithLogging` | `LoggingLayer` | `LoggingLayerOptions` |
| `WithOtelMetrics` | `OtelMetricsLayer` | `OtelMetricsLayerOptions` |
| `WithOtelTrace` | `OtelTraceLayer` | `OtelTraceLayerOptions` |

The layer contract is:

```csharp
public interface IVcsProviderLayer
{
    IVcsProvider Wrap(IVcsProvider inner);
}
```

`VcsProviderDecorator` is the base class for decorators that forward every
operation.

### Layer options

| Options type | Properties |
| --- | --- |
| `RetryLayerOptions` | `MaxAttempts`, `BaseDelay`, `MaxDelay`, `JitterFactor`, `RetryPredicate` |
| `TimeoutLayerOptions` | `Timeout` |
| `ConcurrentLimiterLayerOptions` | `MaxConcurrentRequests`, `AcquireTimeout` |
| `LoggingLayerOptions` | `ProviderName`, `Logger` |
| `OtelMetricsLayerOptions` | `MeterName`, `MeterVersion`, `MetricPrefix`, `ProviderName` |
| `OtelTraceLayerOptions` | `ActivitySourceName`, `ActivitySourceVersion`, `ProviderName`, `RecordException` |

## Logging types

```csharp
public interface IVcsProviderLogger
{
    void LogStart(VcsProviderLogContext context);
    void LogSuccess(VcsProviderLogContext context, TimeSpan duration);
    void LogFailure(VcsProviderLogContext context, TimeSpan duration, string exceptionType);
}
```

`VcsProviderLogContext` contains `ProviderName`, `Operation`, `Repository`,
`PullRequestNumber`, and `Outcome`. `NullVcsProviderLogger` ignores every event.

## Exceptions

| Exception | Base | Properties |
| --- | --- | --- |
| `VcsProviderException` | `Exception` | `IsTransient` |
| `VcsNotFoundException` | `VcsProviderException` | `Resource` |
| `RepositoryNotFoundException` | `VcsNotFoundException` | `Owner`, `Project`, `Name` |
| `PullRequestNotFoundException` | `VcsNotFoundException` | `Repository`, `Number` |
| `VcsAuthenticationException` | `VcsProviderException` | None |
| `VcsAuthorizationException` | `VcsProviderException` | None |
| `VcsRateLimitException` | `VcsProviderException` | `RetryAfter` |
| `VcsConflictException` | `VcsProviderException` | None |
| `VcsUnsupportedOperationException` | `VcsProviderException` | `Provider`, `Operation` |

See [Pagination and errors](advanced/pagination-and-errors.md) for the
status mapping and retry rules.
