# VcsManager

[![Integration tests](https://img.shields.io/github/check-runs/mrhamburg/VcsManager/main?nameFilter=Integration%20tests&label=integration)](https://github.com/mrhamburg/VcsManager/actions/workflows/ci.yml)

Unified version control system management for .NET.

VcsManager provides a core abstraction package plus one package per supported
provider, so applications can work with GitHub, GitLab, Azure DevOps, and Gitea
through a consistent API.

## Status

The repository is under active development. The core contracts, builder,
configuration readers, operational layers, and tests are available. The GitHub,
GitLab, Azure DevOps, and Gitea providers are available.

Integration tests run each provider against a live service. The table shows the
latest result on `main`. A grey `no check runs` pill means the provider's CI
secrets are not configured, so the job is skipped.

| Provider | Integration test config | Latest status on `main` |
| --- | --- | --- |
| GitHub | [`configs/github.yaml`](tests/VcsManager.IntegrationTests/configs/github.yaml) | [![GitHub integration tests](https://img.shields.io/github/check-runs/mrhamburg/VcsManager/main?nameFilter=Integration%20tests%20%28GitHub%29&label=github)](https://github.com/mrhamburg/VcsManager/actions/workflows/ci.yml) |
| GitLab | [`configs/gitlab.yaml`](tests/VcsManager.IntegrationTests/configs/gitlab.yaml) | [![GitLab integration tests](https://img.shields.io/github/check-runs/mrhamburg/VcsManager/main?nameFilter=Integration%20tests%20%28GitLab%29&label=gitlab)](https://github.com/mrhamburg/VcsManager/actions/workflows/ci.yml) |
| Azure DevOps | [`configs/azure-devops.yaml`](tests/VcsManager.IntegrationTests/configs/azure-devops.yaml) | [![Azure DevOps integration tests](https://img.shields.io/github/check-runs/mrhamburg/VcsManager/main?nameFilter=Integration%20tests%20%28Azure%20DevOps%29&label=azure%20devops)](https://github.com/mrhamburg/VcsManager/actions/workflows/ci.yml) |
| Gitea | [`configs/gitea.yaml`](tests/VcsManager.IntegrationTests/configs/gitea.yaml) | [![Gitea integration tests](https://img.shields.io/github/check-runs/mrhamburg/VcsManager/main?nameFilter=Integration%20tests%20%28Gitea%29&label=gitea)](https://github.com/mrhamburg/VcsManager/actions/workflows/ci.yml) |

## Project layout

```
src/
  VcsManager/              Core abstractions (VcsManager.Core)
  VcsManager.GitHub/       GitHub provider
  VcsManager.GitLab/       GitLab provider
  VcsManager.AzureDevOps/  Azure DevOps provider
  VcsManager.Gitea/        Gitea provider
tests/
  VcsManager.Tests/
  VcsManager.GitHub.Tests/
  VcsManager.GitLab.Tests/
  VcsManager.AzureDevOps.Tests/
  VcsManager.Gitea.Tests/
  VcsManager.IntegrationTests/
samples/
docs/                      Docusaurus documentation site
```

## Requirements

- .NET 10 SDK or later
- Linux, macOS, or Windows
- Node.js 20 or later for the documentation site

## Build and test

```bash
dotnet restore
dotnet build
dotnet test
```

Integration tests are a console runner with YAML configs. See
[`tests/VcsManager.IntegrationTests/README.md`](tests/VcsManager.IntegrationTests/README.md).

## Package

```bash
dotnet pack src/VcsManager/VcsManager.csproj -c Release
```

The core package ID is `VcsManager.Core`.

## Sample

The sample runs the full repository and pull request flow with the built-in
in-memory `demo` provider. For the same flow against a real provider, see the
[GitHub provider page](docs/providers/github.md), the
[GitLab provider page](docs/providers/gitlab.md), the
[Azure DevOps provider page](docs/providers/azure-devops.md), or the
[Gitea provider page](docs/providers/gitea.md).

```bash
cd samples/VcsManager.Samples
VCS_PROVIDER=demo dotnet run
```

Optional owner:

```bash
VCS_PROVIDER=demo VCS_DEMO_OWNER=my-owner dotnet run
```

YAML configuration:

```bash
VCS_CONFIG_FILE=./sample.yaml dotnet run
```

```yaml
provider:
  demo: {}
```

The sample exits with status `1` when no configuration is present, before it
runs any operation.

## Configuration

Use `VcsProviderBuilder` with `UseProvider`, environment variables, or YAML.

Environment variables use this shape:

```bash
VCS_PROVIDER=github
VCS_GITHUB_TOKEN=<token>
VCS_GITHUB_BASE_URL=https://github.example.com/api/v3
```

`VCS_{PROVIDER}_{KEY}` maps to a setting key. Underscores after the provider
name become dots, so `VCS_GITHUB_BASE_URL` becomes `base.url`.

YAML uses one provider block:

```yaml
provider:
  github:
    token: <token>
    base:
      url: https://github.example.com/api/v3
```

## Documentation

The documentation site is built with Docusaurus:

<https://mrhamburg.github.io/VcsManager/>

Run it locally:

```bash
cd docs
npm install
npm start
```

Build the static site:

```bash
cd docs
npm run build
```

Source documentation:

- [Introduction](docs/intro.md)
- [Installation](docs/getting-started/installation.md)
- [Quick start](docs/getting-started/quick-start.md)
- [Configuration](docs/getting-started/configuration.md)
- [Providers overview](docs/providers/overview.md)
- [Provider capability matrix](docs/providers/capability-matrix.md)
- [GitHub provider](docs/providers/github.md)
- [GitLab provider](docs/providers/gitlab.md)
- [Azure DevOps provider](docs/providers/azure-devops.md)
- [Gitea provider](docs/providers/gitea.md)
- [Advanced usage](docs/advanced/layers.md)
- [API reference](docs/api-reference.md)
- [Architecture decision records](docs/adr/README.md)

## Contributing

- [Contributing guide](CONTRIBUTING.md)
- [Security policy](SECURITY.md)
- [Bug report template](.github/ISSUE_TEMPLATE/bug_report.md)
- [Feature request template](.github/ISSUE_TEMPLATE/feature_request.md)

## License

Licensed under either of the following, at your option:

- MIT License ([LICENSE-MIT](LICENSE-MIT))
- Apache License 2.0 ([LICENSE-APACHE](LICENSE-APACHE))
