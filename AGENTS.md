# AGENTS.md

Guidance for AI coding agents working in the VcsManager repository.

## Project

VcsManager is a .NET library for unified version control system management. It
provides a core abstraction package plus one package per provider so callers can
work with GitHub, GitLab, Azure DevOps, and Gitea through the same API.

The core contracts, builder, configuration readers, operational layers, and all
four providers are implemented. The repository uses trunk-based development and
is pre-1.0.

The repository structure and build approach follow
[`mrhamburg/SecretsManager`](https://github.com/mrhamburg/SecretsManager).

## Read first

Before changing code or design, read:

1. [`docs/adr/README.md`](docs/adr/README.md) — architecture decisions. These are
   binding.
2. [`docs/providers/capability-matrix.md`](docs/providers/capability-matrix.md) —
   supported, conditional, and unsupported provider behavior.
3. [`README.md`](README.md) — layout and commands.

## Repository layout

```
src/
  VcsManager/              Core abstractions; package ID VcsManager.Core
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
  adr/                     Architecture decisions (site content)
  providers/               Provider guides (site content)
  getting-started/         Installation, quick start, configuration
  advanced/                Layers, custom providers, paging, merge
  api-reference.md         Public API summary
```

One test project exists per package. Integration tests live in
`VcsManager.IntegrationTests`.

## Build and verification

Requirements: .NET 10 SDK on Linux, macOS, or Windows.

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes --no-restore
dotnet pack src/VcsManager/VcsManager.csproj -c Release
```

Documentation site (Node.js 20 or later):

```bash
cd docs
npm ci
npm run build
```

Run restore, build, test, and format before pushing. For documentation-only
changes, build and test are not strictly required, but the documentation build
must succeed and format is cheap and should still pass.

## Binding architecture decisions

Do not contradict these without adding a new ADR. Accepted ADRs are not
rewritten.

| ADR | Rule |
| --- | --- |
| [0001](docs/adr/0001-repository-addressing.md) | `RepositoryRef` is the only repository identity. `Name` is required; `Owner` and `Project` are optional. Providers reject missing required fields and meaningless fields. |
| [0002](docs/adr/0002-authentication.md) | PAT authentication only in v1. Callers supply tokens. Never log, persist, or commit tokens. |
| [0003](docs/adr/0003-github-client.md) | GitHub uses Octokit 14.0.0, pinned exactly, behind an internal `IGitHubApiClient`. No Octokit type in the public API. |
| [0004](docs/adr/0004-gitlab-client.md) | GitLab uses REST API v4 with `HttpClient` and `System.Text.Json` behind an internal `IGitLabApiClient`. No third-party GitLab SDK. |
| [0005](docs/adr/0005-azure-devops-client.md) | Azure DevOps uses `Microsoft.TeamFoundationServer.Client` 20.256.2 as the primary client, with REST API 7.1 as the documented fallback. Reviewer votes and completion are separate operations. |
| [0006](docs/adr/0006-repository-list-scope.md) | Listing returns repositories visible to the token within the configured owner, namespace, or project scope. `IncludeArchived` defaults to `false`. |
| [0007](docs/adr/0007-merge-method-mapping.md) | `Merge` → GitHub `merge`, GitLab merge commit, Azure DevOps `noFastForward`. `Squash` → squash. `Rebase` → rebase, conditional on GitLab project settings. |
| [0008](docs/adr/0008-close-semantics.md) | Close never merges. Close is idempotent: closed, merged, and completed pull requests are returned unchanged. |
| [0009](docs/adr/0009-pagination-and-errors.md) | Page defaults to 1; page size defaults to 30 and must be 1–100. `TotalCount` is nullable. Map provider errors to the core error contract and never leak secrets. |
| [0010](docs/adr/0010-exception-contract.md) | Provider failures derive from `VcsProviderException`. Never leak provider SDK or raw HTTP exceptions. Authentication and authorization are distinct; rate limits expose `RetryAfter`. |
| [0011](docs/adr/0011-gitea-client.md) | Gitea uses REST API v1 with `HttpClient` and `System.Text.Json` behind an internal client. The page size is capped at 50. |
| [0012](docs/adr/0012-provider-listing-and-merge-scope.md) | GitHub and GitLab list organization/group scope when `Owner` is set. GitLab rejects the `Rebase` merge method in v1. |

## Code conventions

- Target `net10.0` with nullable reference types, implicit usings, and the
  latest C# language version, as set in `Directory.Build.props`.
- Keep the core project free of provider-specific dependencies. Do not add a
  provider SDK, Octokit, or Azure DevOps package to `VcsManager`.
- Keep provider-native types and clients internal. Public APIs use core records
  and enums only.
- Public types and members require XML documentation (see issue #3).
- Prefer immutable `record` types and `required`/`init` accessors.
- Keep implementations simple and minimal. Do not add abstractions,
  configuration, or future-proofing that the current issue does not need.
- Follow `.editorconfig`. Run `dotnet format` before pushing.

## Testing

- Test projects use xUnit, `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`,
  `coverlet.collector`, and NSubstitute.
- Unit tests replace internal provider clients; they do not call real providers.
- Integration tests use the console runner in
  `tests/VcsManager.IntegrationTests` with YAML configs under `configs/`. They
  read credentials from the config or CI secrets. Never commit tokens or test
  data containing secrets. The runner never prints tokens.
- CI runs the Gitea integration job. GitHub, GitLab, and Azure DevOps jobs are
  present but disabled by `HAS_*_SECRETS` repository variables.
- Cover validation, provider-to-core mapping, merge-method mapping, close
  idempotency, pagination metadata, and error mapping.
- Do not add tests for documentation-only changes.

## Documentation

- Record a changed design decision in a new ADR under `docs/adr/`. Add it to the
  index in `docs/adr/README.md`.
- Keep `docs/providers/capability-matrix.md` in sync with provider behavior.
- Update `README.md` when commands, layout, or requirements change.
- Keep the Docusaurus content under `docs/` in sync with provider behavior and
  the public API.
- Each ADR needs Context, Decision, and Consequences sections and links to the
  official provider documentation.

## Workflow

- This repository uses trunk-based development. Work on `main` or on an issue
  branch that is fast-forwarded to `main` before pushing.
- Use conventional commit messages and include the issue number, for example
  `feat: add core domain model (#3)`.
- Do not add `Co-Authored-By` trailers or AI/tool attribution.
- Do not commit generated output, build artifacts, or secrets.
- If git identity is not configured, use the repository's existing author
  identity with `git -c user.name=... -c user.email=...` for the commit. Do not
  change global git configuration.

## Security

- Never add tokens, credentials, private keys, authorization headers, or secret
  values to code, tests, logs, documentation, or commit messages.
- Credentials come from a local integration config or CI secrets. Never commit
  them. Unit tests must not require credentials.
- Error messages must not include tokens or full request bodies.
