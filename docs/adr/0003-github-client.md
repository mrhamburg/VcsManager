# ADR-0003: GitHub uses Octokit behind an internal client interface

- Status: Accepted
- Date: 2026-10-25

## Context

The GitHub provider needs typed access to repositories and pull requests. A
hand-written REST client would duplicate pagination, authentication, and
response models that Octokit already maintains. Coupling the public VcsManager
API to Octokit would leak a provider SDK type into every caller.

## Decision

The `VcsManager.GitHub` package uses **Octokit 14.0.0**, pinned to that exact
version.

- The provider defines an internal `IGitHubApiClient` interface. Its only
  implementation wraps `Octokit.GitHubClient`.
- Provider logic depends on the internal interface, so unit tests can replace it
  without an HTTP server.
- Octokit types are converted inside the provider. No Octokit type appears in
  the core project or in the public VcsManager API.
- The Octokit version is updated deliberately. A version change is a normal
  dependency update, validated by the provider unit and integration tests.

## Consequences

- The provider inherits Octokit's authentication, rate-limit, and pagination
  handling.
- Callers can change providers without recompiling against Octokit.
- Octokit 14.0.0 targets .NET Standard 2.0 and is usable from `net10.0`.
- If Octokit is replaced later, only the internal client implementation and its
  tests change.

## References

- Octokit 14.0.0 package: [https://www.nuget.org/packages/Octokit/14.0.0](https://www.nuget.org/packages/Octokit/14.0.0)
- Octokit.NET source: [https://github.com/octokit/octokit.net](https://github.com/octokit/octokit.net)
- GitHub REST API for repositories: [https://docs.github.com/en/rest/repos/repos](https://docs.github.com/en/rest/repos/repos)
- GitHub REST API for pull requests: [https://docs.github.com/en/rest/pulls/pulls](https://docs.github.com/en/rest/pulls/pulls)
