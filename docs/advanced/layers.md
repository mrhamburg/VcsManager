---
id: layers
title: Layering and Decorators
sidebar_label: Layering
sidebar_position: 1
---

# Layering and Decorators

Layers add cross-cutting behavior around any provider. They implement
`IVcsProviderLayer`, wrap an `IVcsProvider`, and forward every operation.

```csharp
using VcsManager;
using VcsManager.Builder;

var provider = new VcsProviderBuilder()
    .WithGitHub(options => options.Token = token)
    .WithRetry()
    .WithTimeout(options => options.Timeout = TimeSpan.FromSeconds(5))
    .WithConcurrentLimiter(options => options.MaxConcurrentRequests = 5)
    .WithLogging(options => options.ProviderName = "github")
    .WithOtelMetrics()
    .WithOtelTrace()
    .Build();
```

## Order matters

`VcsProviderBuilder` applies layers in registration order. The first layer
added is the outermost wrapper.

```csharp
var provider = new VcsProviderBuilder()
    .WithGitHub(options => options.Token = token)
    .WithRetry()    // outermost
    .WithTimeout()  // applied to each retry attempt
    .Build();
```

If timeout is added first, it becomes the outermost layer and limits the total
operation, including all retry attempts:

```csharp
.WithTimeout()
.WithRetry() // inside the timeout
```

Choose the order that matches the policy you need.

## Retry

```csharp
var provider = new VcsProviderBuilder()
    .WithGitHub(options => options.Token = token)
    .WithRetry(options =>
    {
        options.MaxAttempts = 5;
        options.BaseDelay = TimeSpan.FromMilliseconds(100);
        options.MaxDelay = TimeSpan.FromSeconds(2);
        options.JitterFactor = 0.2;
        options.RetryPredicate = exception => exception is not VcsAuthorizationException;
    })
    .Build();
```

| Option | Default | Description |
| --- | --- | --- |
| `MaxAttempts` | `3` | Total attempts, including the first. |
| `BaseDelay` | `100 ms` | Base delay for exponential backoff. |
| `MaxDelay` | `2 s` | Maximum delay between attempts. |
| `JitterFactor` | `0.2` | Random jitter from `0` to `1`. |
| `RetryPredicate` | `null` | Optional veto. It cannot make a non-retryable failure retryable. |

Retry behavior:

- Rate limits are retried. `VcsRateLimitException.RetryAfter` is used as the
  delay when present.
- Transport failures that surface as an `HttpRequestException` with a 429 or
  5xx status are retried. Provider responses that become `VcsProviderException`
  are only retried when their inner exception is such an `HttpRequestException`.
- `VcsConflictException` and `VcsUnsupportedOperationException` are not retried.
- Canceled operations are not retried.
- Merge is never retried because it is not automatically idempotent.
- Create repository, delete repository, and create pull request are not
  retried because a retry can duplicate the operation.
- Read operations and close are retried. Close is idempotent by contract.
- A successful operation returns immediately; the layer does not retry it.

## Timeout

```csharp
var provider = new VcsProviderBuilder()
    .WithGitLab(options => options.Token = token)
    .WithTimeout(options => options.Timeout = TimeSpan.FromSeconds(5))
    .Build();
```

`TimeoutLayer` applies a deadline to each provider operation. It throws
`TimeoutException` when the deadline expires and the caller did not cancel the
operation. The default timeout is 10 seconds.

## Concurrency limiter

```csharp
var provider = new VcsProviderBuilder()
    .WithAzureDevOps(options => options.PersonalAccessToken = token)
    .WithConcurrentLimiter(options =>
    {
        options.MaxConcurrentRequests = 5;
        options.AcquireTimeout = TimeSpan.FromMilliseconds(200);
    })
    .Build();
```

| Option | Default | Description |
| --- | --- | --- |
| `MaxConcurrentRequests` | `10` | Maximum operations that run at the same time. |
| `AcquireTimeout` | `null` | Maximum wait for a slot. `null` waits indefinitely. |

When `AcquireTimeout` expires, the layer throws `TimeoutException` unless the
caller canceled the operation.

## Logging

```csharp
public sealed class ConsoleVcsLogger : IVcsProviderLogger
{
    public void LogStart(VcsProviderLogContext context) =>
        Console.WriteLine($"{context.ProviderName} {context.Operation} start");

    public void LogSuccess(VcsProviderLogContext context, TimeSpan duration) =>
        Console.WriteLine($"{context.Operation} success in {duration}");

    public void LogFailure(VcsProviderLogContext context, TimeSpan duration, string exceptionType) =>
        Console.WriteLine($"{context.Operation} failed with {exceptionType}");
}

var provider = new VcsProviderBuilder()
    .WithGitea(options => options.BaseUrl = baseUrl)
    .WithLogging(options =>
    {
        options.ProviderName = "gitea";
        options.Logger = new ConsoleVcsLogger();
    })
    .Build();
```

`VcsProviderLogContext` contains the provider name, operation, optional
repository display name, optional pull request number, and outcome. It never
contains tokens, authorization headers, request bodies, or exception messages.
`NullVcsProviderLogger` ignores every event and is the default.

## OpenTelemetry metrics

```csharp
var provider = new VcsProviderBuilder()
    .WithGitHub(options => options.Token = token)
    .WithOtelMetrics(options =>
    {
        options.MeterName = "my-app.vcs";
        options.MeterVersion = "1.0.0";
        options.MetricPrefix = "vcs.";
        options.ProviderName = "github";
    })
    .Build();
```

The layer emits:

| Metric | Type | Description |
| --- | --- | --- |
| `{prefix}operation.calls` | Counter | Provider operations started. |
| `{prefix}operation.failures` | Counter | Failed provider operations. |
| `{prefix}operation.duration_ms` | Histogram | Operation duration in milliseconds. |

Tags include `provider`, `operation`, optional `repository`, optional
`pull_request`, and `outcome` (`success` or `failure`).

## OpenTelemetry trace

```csharp
var provider = new VcsProviderBuilder()
    .WithGitLab(options => options.Token = token)
    .WithOtelTrace(options =>
    {
        options.ActivitySourceName = "my-app.vcs";
        options.ProviderName = "gitlab";
        options.RecordException = true;
    })
    .Build();
```

Each call creates a client activity named after the operation. Tags include
`vcs.provider`, `vcs.operation`, optional `vcs.repository`, and optional
`vcs.pull_request.number`. Failures set an error status with the exception type;
exception messages are never recorded.

## Custom layers

Implement `IVcsProviderLayer` and wrap the provider:

```csharp
public sealed class AuditLayer : IVcsProviderLayer
{
    public IVcsProvider Wrap(IVcsProvider inner) => new AuditProvider(inner);
}

public sealed class AuditProvider(IVcsProvider inner) : VcsProviderDecorator(inner)
{
    public override async Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Merging #{number} in {repository.Name}");
        return await Inner.MergePullRequestAsync(repository, number, request, cancellationToken);
    }
}
```

Register the layer with `AddLayer`:

```csharp
var provider = new VcsProviderBuilder()
    .WithGitHub(options => options.Token = token)
    .AddLayer(new AuditLayer())
    .Build();
```
