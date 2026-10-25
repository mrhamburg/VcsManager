---
id: quick-start
title: Quick Start
sidebar_label: Quick Start
sidebar_position: 2
---

# Quick Start

This guide runs the main repository and pull request flow against GitHub. The
same flow works with the other providers after you change the registration
call and the credentials.

## Prerequisites

- .NET 10 SDK.
- A GitHub repository you can write to.
- A personal access token with repository and pull request permissions.
- Two branches that already exist in the repository, for example
  `feature/health` and `main`.

## Step 1: Install packages

```bash title="Terminal"
dotnet add package VcsManager.Core
dotnet add package VcsManager.GitHub
```

## Step 2: Configure the provider

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
```

The builder selects exactly one provider. To use two providers in the same
application, build two provider instances.

## Step 3: Run the repository and pull request flow

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

var current = await provider.GetRepositoryAsync(repository);
Console.WriteLine($"Repository: {current.FullName}");

var page = await provider.ListRepositoriesAsync(
    new RepositoryQuery { Owner = repository.Owner, NameContains = repository.Name },
    new PaginationOptions { PageSize = 10 });
Console.WriteLine($"Matched {page.Items.Count} repositories");

var pullRequest = await provider.CreatePullRequestAsync(
    repository,
    new CreatePullRequestRequest
    {
        Title = "Add health endpoint",
        Description = "Adds a liveness probe.",
        SourceBranch = "feature/health",
        TargetBranch = "main",
    });
Console.WriteLine($"Created pull request #{pullRequest.Number}");

var merged = await provider.MergePullRequestAsync(
    repository,
    pullRequest.Number,
    new MergePullRequestRequest
    {
        Method = MergeMethod.Squash,
        DeleteSourceBranch = true,
        CommitTitle = "Add health endpoint",
    });
Console.WriteLine($"Merged as {merged.State}");
```

### Create and delete a repository

Create a new repository when you need one:

```csharp
var created = await provider.CreateRepositoryAsync(new CreateRepositoryRequest
{
    Owner = "my-org",
    Name = "temporary-repository",
    Description = "Created by VcsManager",
    InitializeWithReadme = true,
});
```

### Seed a repository from a template folder

Create an empty repository, then seed its first commit from a local folder:

```csharp
var templateDirectory = "templates/service";
var files = Directory
    .EnumerateFiles(templateDirectory, "*", SearchOption.AllDirectories)
    .Select(path => new RepositorySeedFile
    {
        Path = Path.GetRelativePath(templateDirectory, path).Replace('\\', '/'),
        Content = File.ReadAllBytes(path),
    })
    .ToArray();

await provider.SeedRepositoryAsync(
    new RepositoryRef { Owner = "my-org", Name = "new-service" },
    new SeedRepositoryRequest
    {
        Branch = "main",
        CommitMessage = "Create service from template",
        Files = files,
    });
```

The repository must be empty and the caller must set `InitializeWithReadme = false`
when it creates the repository. The seeded branch becomes the default branch.

:::warning[Repository deletion is permanent]
`DeleteRepositoryAsync` removes the repository and its history. It is not
idempotent: a second delete returns `RepositoryNotFoundException`. Use it only
when the repository is disposable.
:::

```csharp
await provider.DeleteRepositoryAsync(new RepositoryRef
{
    Owner = "my-org",
    Name = "temporary-repository",
});
```

## Environment-variable configuration

The same provider can be configured without code changes:

```bash title="Terminal"
export VCS_PROVIDER=github
export VCS_GITHUB_TOKEN=<token>
export VCS_GITHUB_DEFAULT_OWNER=my-org
```

```csharp
var provider = new VcsProviderBuilder()
    .WithGitHub()
    .FromEnvironment()
    .Build();
```

`VCS_{PROVIDER}_{KEY}` maps to a setting key. Underscores after the provider
name become dots, so `VCS_GITHUB_DEFAULT_OWNER` becomes `default.owner`.

## Use dependency injection

```csharp title="Program.cs"
using VcsManager;
using VcsManager.Builder;
using VcsManager.GitHub;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IVcsProvider>(_ =>
    new VcsProviderBuilder()
        .WithGitHub(options =>
        {
            options.Token = builder.Configuration["GitHub:Token"];
            options.DefaultOwner = builder.Configuration["GitHub:Owner"];
        })
        .Build());

var app = builder.Build();
app.Run();
```

The service container disposes the singleton provider when the host stops.

## Handle errors

```csharp
try
{
    var repository = await provider.GetRepositoryAsync(
        new RepositoryRef { Owner = "my-org", Name = "missing" });
}
catch (RepositoryNotFoundException)
{
    Console.WriteLine("Repository does not exist.");
}
catch (VcsAuthenticationException)
{
    Console.WriteLine("The token is missing or invalid.");
}
catch (VcsRateLimitException exception)
{
    Console.WriteLine($"Rate limited. Retry after {exception.RetryAfter}.");
}
catch (VcsProviderException exception)
{
    Console.WriteLine($"Provider failure: {exception.Message}");
}
```

## Iterate over pages

Each listing call returns one page. Continue while `HasNextPage` is `true`:

```csharp
var pageNumber = PaginationOptions.DefaultPage;

while (true)
{
    var result = await provider.ListPullRequestsAsync(
        repository,
        new PullRequestQuery { State = PullRequestState.Open },
        new PaginationOptions { Page = pageNumber, PageSize = 50 });

    foreach (var pullRequest in result.Items)
    {
        Console.WriteLine($"#{pullRequest.Number} {pullRequest.Title}");
    }

    if (!result.HasNextPage)
    {
        break;
    }

    pageNumber++;
}
```

## Next steps

- [Configuration](configuration.md).
- [Choose a provider](../providers/overview.md).
- [Add operational layers](../advanced/layers.md).
- [Understand pagination and errors](../advanced/pagination-and-errors.md).
