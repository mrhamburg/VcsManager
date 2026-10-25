# Contributing

Thanks for your interest in VcsManager. This guide explains how to build,
test, and submit changes.

## Requirements

- .NET 10 SDK
- Linux, macOS, or Windows
- Git

## Setup

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes --no-restore
```

Run restore, build, test, and format before you open a pull request.

## Workflow

- This repository uses trunk-based development.
- Work on `main` or on an issue branch that is fast-forwarded to `main` before
  you push.
- Use conventional commit messages and include the issue number, for example
  `feat: add core domain model (#3)`.
- Do not add `Co-Authored-By` trailers or AI/tool attribution.
- Keep each pull request focused on one issue or one small change.

## Code conventions

- Target `net10.0` with nullable reference types, implicit usings, and the
  latest C# language version.
- Keep the core project free of provider-specific dependencies.
- Keep provider-native types and clients internal.
- Public types and members need XML documentation.
- Prefer immutable `record` types and `required`/`init` accessors.
- Follow `.editorconfig`.
- Read [`AGENTS.md`](AGENTS.md), the [ADRs](docs/adr/README.md), and the
  [capability matrix](docs/providers/capability-matrix.md) before you change
  behavior.

## Tests

- Test projects use xUnit, NSubstitute, `Microsoft.NET.Test.Sdk`, and
  `coverlet.collector`.
- Unit tests must not call real providers.
- Integration tests use credentials from environment variables and skip when
  the variables are missing.
- Cover validation, provider mapping, merge behavior, close idempotency,
  pagination, and error mapping.
- Add a regression test for each bug fix.

## Documentation site

The Docusaurus site uses Node.js 20 or later:

```bash
cd docs
npm ci
npm run build
```

The build must succeed for documentation changes. The site reads the Markdown
files under `docs/` directly, so update the source pages rather than a separate
copy.

## Documentation

- Record a changed design decision in a new ADR under `docs/adr/`. Accepted
  ADRs are not rewritten.
- Update the ADR index and the provider capability matrix when behavior
  changes.
- Update `README.md` when commands, layout, or requirements change.

## Security

- Never commit tokens, credentials, private keys, authorization headers, or
  secret values.
- Do not include secrets in issues, pull requests, logs, or test data.
- Report vulnerabilities according to [`SECURITY.md`](SECURITY.md).

## Pull requests

Use the pull request template. Confirm that formatting, build, and tests pass,
and that the change contains no secrets.
