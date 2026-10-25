---
id: custom-providers
title: Building Custom Providers
sidebar_label: Custom Providers
sidebar_position: 2
---

# Building Custom Providers

A custom provider implements `IVcsProvider` and registers an
`IVcsProviderFactory` with the builder. This lets the same application code use
another version control system without changing the core contract.

## Implement the provider

Implement every member of `IVcsProvider`. Use `VcsProviderDecorator` when you
only need to change a few operations; use `IVcsProvider` directly for a new
backend.

```csharp title="AcmeVcsProvider.cs"
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VcsManager;

public sealed class AcmeVcsProvider : IVcsProvider
{
    private readonly HttpClient _http;

    public AcmeVcsProvider(string baseUrl, string token)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<VcsRepository> GetRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (string.IsNullOrWhiteSpace(repository.Owner))
        {
            throw new ArgumentException("Acme requires RepositoryRef.Owner.", nameof(repository));
        }

        var path = $"{Uri.EscapeDataString(repository.Owner)}/{Uri.EscapeDataString(repository.Name)}";
        using var response = await _http.GetAsync($"api/repos/{path}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new RepositoryNotFoundException(repository.Owner, repository.Project, repository.Name);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new VcsAuthenticationException("Acme rejected the token.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new VcsAuthorizationException("Acme denied access to the repository.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new VcsProviderException(
                $"Acme returned {(int)response.StatusCode} for GetRepository.");
        }

        var dto = await response.Content.ReadFromJsonAsync<AcmeRepository>(cancellationToken)
            ?? throw new VcsProviderException("Acme returned an empty repository response.");

        return new VcsRepository
        {
            Id = dto.Id,
            Name = dto.Name,
            Owner = dto.Owner,
            FullName = $"{dto.Owner}/{dto.Name}",
            Url = dto.Url,
            DefaultBranch = dto.DefaultBranch,
            Visibility = dto.Private ? VcsVisibility.Private : VcsVisibility.Public,
            Description = dto.Description,
        };
    }

    public Task<VcsRepository> CreateRepositoryAsync(
        CreateRepositoryRequest request,
        CancellationToken cancellationToken = default) =>
        throw new VcsUnsupportedOperationException("acme", nameof(CreateRepositoryAsync));

    public Task<PagedResult<VcsRepository>> ListRepositoriesAsync(
        RepositoryQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default) =>
        throw new VcsUnsupportedOperationException("acme", nameof(ListRepositoriesAsync));

    public Task DeleteRepositoryAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default) =>
        throw new VcsUnsupportedOperationException("acme", nameof(DeleteRepositoryAsync));

    public Task<bool> RepositoryExistsAsync(
        RepositoryRef repository,
        CancellationToken cancellationToken = default) =>
        throw new VcsUnsupportedOperationException("acme", nameof(RepositoryExistsAsync));

    public Task<PullRequest> CreatePullRequestAsync(
        RepositoryRef repository,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken = default) =>
        throw new VcsUnsupportedOperationException("acme", nameof(CreatePullRequestAsync));

    public Task<PullRequest> GetPullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default) =>
        throw new VcsUnsupportedOperationException("acme", nameof(GetPullRequestAsync));

    public Task<PagedResult<PullRequest>> ListPullRequestsAsync(
        RepositoryRef repository,
        PullRequestQuery? query = null,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default) =>
        throw new VcsUnsupportedOperationException("acme", nameof(ListPullRequestsAsync));

    public Task<PullRequest> MergePullRequestAsync(
        RepositoryRef repository,
        int number,
        MergePullRequestRequest? request = null,
        CancellationToken cancellationToken = default) =>
        throw new VcsUnsupportedOperationException("acme", nameof(MergePullRequestAsync));

    public Task<PullRequest> ClosePullRequestAsync(
        RepositoryRef repository,
        int number,
        CancellationToken cancellationToken = default) =>
        throw new VcsUnsupportedOperationException("acme", nameof(ClosePullRequestAsync));

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed record AcmeRepository(
        string Id,
        string Name,
        string Owner,
        string Url,
        string? DefaultBranch,
        string? Description,
        bool Private);
}
```

## Implement the factory

```csharp title="AcmeVcsProviderFactory.cs"
using VcsManager;
using VcsManager.Builder;

public sealed class AcmeVcsProviderFactory : IVcsProviderFactory
{
    public string ProviderName => "acme";

    public IVcsProvider Create(IReadOnlyDictionary<string, string> configuration)
    {
        if (!configuration.TryGetValue("base.url", out var baseUrl) ||
            string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Acme requires the 'base.url' setting.");
        }

        if (!configuration.TryGetValue("token", out var token) ||
            string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Acme requires the 'token' setting.");
        }

        return new AcmeVcsProvider(baseUrl, token);
    }
}
```

## Register the provider

```csharp
using VcsManager.Builder;

var provider = new VcsProviderBuilder()
    .RegisterProvider(new AcmeVcsProviderFactory())
    .UseProvider("acme", settings =>
    {
        settings["base.url"] = "https://acme.example.com";
        settings["token"] = token;
    })
    .Build();
```

`RegisterProvider` makes the name available. `UseProvider`, `FromEnvironment`,
`FromYaml`, or `FromYamlFile` selects it and supplies settings.

## Add a fluent extension

A provider package usually exposes an extension method:

```csharp title="AcmeVcsProviderBuilderExtensions.cs"
using VcsManager.Builder;

public sealed class AcmeOptions
{
    public string? BaseUrl { get; set; }
    public string? Token { get; set; }
}

public static class AcmeVcsProviderBuilderExtensions
{
    public static VcsProviderBuilder WithAcme(
        this VcsProviderBuilder builder,
        Action<AcmeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterProvider(new AcmeVcsProviderFactory());

        if (configure is null)
        {
            return builder;
        }

        var options = new AcmeOptions();
        configure(options);

        builder.UseProvider("acme", settings =>
        {
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                settings["base.url"] = options.BaseUrl;
            }

            if (!string.IsNullOrWhiteSpace(options.Token))
            {
                settings["token"] = options.Token;
            }
        });

        return builder;
    }
}
```

## Contract rules

- Validate required fields and reject meaningless fields. Do not silently
  ignore `Project` when the provider has no project level.
- Map provider failures to the core exception family. Preserve the original
  exception as the inner exception when possible.
- Never include tokens, authorization headers, or full request bodies in an
  exception message.
- Respect the caller's `CancellationToken`.
- Return one page per call. Set `Page`, `PageSize`, `TotalCount`, and
  `HasNextPage` correctly. Use `null` for `TotalCount` when the provider does
  not report one.
- Do not retry non-idempotent operations inside the provider. Use the retry
  layer if the caller wants retry behavior.
- Implement `DisposeAsync` and release provider clients.

## Reference implementations

Read the provider packages for complete examples:

- GitHub: Octokit behind an internal client.
- GitLab: REST API v4 with `HttpClient` and `System.Text.Json`.
- Azure DevOps: official SDK with REST 7.1 fallback.
- Gitea: REST API v1 with `HttpClient` and `System.Text.Json`.
