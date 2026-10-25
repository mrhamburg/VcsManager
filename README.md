# VcsManager

Unified version control system management for .NET.

VcsManager provides a core abstraction package plus one package per supported
provider, so applications can work with GitHub, GitLab, and Azure DevOps
through a consistent API.

## Status

The repository is under active development. The current milestone provides the
solution scaffold and build baseline; provider implementations are not
available yet.

## Project layout

```
src/
  VcsManager/              Core abstractions (VcsManager.Core)
  VcsManager.GitHub/       GitHub provider
  VcsManager.GitLab/       GitLab provider
  VcsManager.AzureDevOps/  Azure DevOps provider
tests/
  VcsManager.Tests/
  VcsManager.GitHub.Tests/
  VcsManager.GitLab.Tests/
  VcsManager.AzureDevOps.Tests/
  VcsManager.IntegrationTests/
samples/
```

## Requirements

- .NET 10 SDK or later
- Linux, macOS, or Windows

## Build and test

```bash
dotnet restore
dotnet build
dotnet test
```

## Package

```bash
dotnet pack src/VcsManager/VcsManager.csproj -c Release
```

The core package ID is `VcsManager.Core`.

## Configuration

Use `VcsProviderBuilder` with `UseProvider`, environment variables, or YAML.

Environment variables use this shape:

```bash
VCS_PROVIDER=github
VCS_GITHUB_TOKEN=<token>
VCS_GITHUB_BASE_URL=https://github.example.com
```

`VCS_{PROVIDER}_{KEY}` maps to a setting key. Underscores after the provider
name become dots, so `VCS_GITHUB_BASE_URL` becomes `base.url`.

YAML uses one provider block:

```yaml
provider:
  github:
    token: <token>
    base:
      url: https://github.example.com
```

## Documentation

- [Architecture decision records](docs/adr/README.md)
- [Provider capability matrix](docs/providers/capability-matrix.md)

## License

Licensed under either of the following, at your option:

- MIT License ([LICENSE-MIT](LICENSE-MIT))
- Apache License 2.0 ([LICENSE-APACHE](LICENSE-APACHE))
