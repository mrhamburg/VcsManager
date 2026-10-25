---
id: configuration
title: Configuration
sidebar_label: Configuration
sidebar_position: 3
---

# Configuration

VcsManager supports three configuration sources: the fluent API, environment
variables, and YAML. Every source produces the same flat settings dictionary
and selects exactly one provider.

## Fluent API

```csharp title="Program.cs"
using VcsManager.Builder;
using VcsManager.GitHub;

var provider = new VcsProviderBuilder()
    .WithGitHub(options =>
    {
        options.Token = Environment.GetEnvironmentVariable("VCS_GITHUB_TOKEN");
        options.BaseUrl = "https://api.github.com";
        options.DefaultOwner = "my-org";
    })
    .Build();
```

Each `WithGitHub`, `WithGitLab`, `WithAzureDevOps`, and `WithGitea` overload
does two things:

1. Registers the provider factory.
2. When an options callback is supplied, selects the provider and converts the
   options to flat settings.

Call the overload without a callback when configuration comes from another
source:

```csharp
var builder = new VcsProviderBuilder()
    .WithGitHub(); // register only
```

## Environment variables

The default prefix is `VCS`:

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

Mapping rules:

- `VCS_PROVIDER` selects the provider name.
- `VCS_{PROVIDER}_{KEY}` supplies a setting.
- The key is lowercased and underscores become dots, so
  `VCS_GITHUB_DEFAULT_OWNER` becomes `default.owner`.
- Empty values are ignored.

Use a custom prefix when `VCS` conflicts with another application:

```csharp
var provider = new VcsProviderBuilder()
    .WithGitLab()
    .FromEnvironment("CONTOSO")
    .Build();
```

Then set `CONTOSO_PROVIDER=gitlab` and the matching `CONTOSO_GITLAB_*`
variables.

## YAML

```yaml title="vcs.yaml"
provider:
  github:
    token: <token>
    default:
      owner: my-org
```

```csharp
var provider = new VcsProviderBuilder()
    .WithGitHub()
    .FromYamlFile("vcs.yaml")
    .Build();
```

The document must contain a top-level `provider` key with exactly one provider
block. Nested keys are flattened with dots, so the example produces `token` and
`default.owner`.

Load YAML from a string when the source is not a file:

```csharp
var provider = new VcsProviderBuilder()
    .WithGitLab()
    .FromYaml("""
        provider:
          gitlab:
            token: <token>
            default:
              namespace: my-group
        """)
    .Build();
```

## Manual settings

Use `UseProvider` when settings come from a custom store:

```csharp
var provider = new VcsProviderBuilder()
    .WithAzureDevOps()
    .UseProvider("azuredevops", settings =>
    {
        settings["organization.url"] = configuration["AzureDevOps:Url"];
        settings["personal.access.token"] = configuration["AzureDevOps:Token"];
        settings["default.project"] = configuration["AzureDevOps:Project"];
    })
    .Build();
```

## Configuration precedence

The builder stores one configuration object. Every configuration call replaces
it:

- `WithGitHub(options => ...)` replaces the configuration with GitHub settings.
- `FromEnvironment`, `FromYaml`, `FromYamlFile`, and `UseProvider` replace the
  configuration with their own values.

The last configuration call wins. Settings from different sources are not
merged. Choose one source per provider instance, or read the values yourself
and pass them to `UseProvider`.

## Provider settings

| Provider | Required keys | Optional keys |
| --- | --- | --- |
| GitHub | `token` | `base.url`, `default.owner`, `request.timeout` |
| GitLab | `token` | `base.url`, `default.namespace` |
| Azure DevOps | `organization.url`, `personal.access.token` | `default.project`, `api.version` |
| Gitea | `base.url`, `token` | `default.owner` |

See the provider pages for base URL rules, token scopes, and addressing:

- [GitHub](../providers/github.md)
- [GitLab](../providers/gitlab.md)
- [Azure DevOps](../providers/azure-devops.md)
- [Gitea](../providers/gitea.md)

## Multiple providers

A builder selects one provider. To use more than one provider, build and keep
one instance per provider:

```csharp
var github = new VcsProviderBuilder()
    .WithGitHub(options => options.Token = githubToken)
    .Build();

var gitlab = new VcsProviderBuilder()
    .WithGitLab(options => options.Token = gitlabToken)
    .Build();
```

## Security

- Never commit tokens, YAML files containing tokens, or environment dumps.
- Read tokens from a secret store or environment variable at runtime.
- VcsManager does not log tokens, authorization headers, or request bodies.
- A rejected token maps to `VcsAuthenticationException`; a token without
  permission maps to `VcsAuthorizationException`.
