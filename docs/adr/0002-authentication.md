# ADR-0002: Personal access token authentication for v1

- Status: Accepted
- Date: 2026-10-25

## Context

All three providers support several authentication mechanisms: personal access
tokens (PATs), OAuth applications, GitHub Apps, and provider-specific service
principals. Supporting all of them in v1 multiplies configuration, testing, and
token lifecycle work before the core contract is stable.

## Decision

VcsManager v1 supports PAT authentication only.

| Provider | Credential form | Transport |
| --- | --- | --- |
| GitHub | Personal access token (classic or fine-grained) | `Authorization: Bearer <token>` through Octokit credentials |
| GitLab | Personal access token | `PRIVATE-TOKEN: <token>` header |
| Azure DevOps | Personal access token | Basic authentication with an empty user name and the token as the password |

Rules:

- The caller supplies the token through provider configuration. VcsManager does
  not read environment variables, credential stores, or interactive prompts on
  its own.
- Tokens are secrets. They are never logged, included in exception messages, or
  written to test output.
- Token scopes determine what the provider returns and which operations
  succeed. VcsManager does not request scopes or elevate permissions.
- OAuth, GitHub App, Azure AD service principal, and GitLab OAuth
  authentication are deferred. They are additive future options, not changes to
  the PAT contract.

## Consequences

- Configuration stays simple and works for server-to-server automation.
- Token rotation and expiry are the caller's responsibility. A rejected token
  maps to an authentication error, not a silent retry.
- Tests use fake tokens only; integration tests read tokens from environment
  variables and never commit them.
- Adding another authentication mode later requires a new ADR and a
  configuration extension, not a breaking change to provider methods.

## References

- GitHub personal access tokens: <https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens>
- GitLab personal access tokens: <https://docs.gitlab.com/user/profile/personal_access_tokens/>
- Azure DevOps personal access tokens: <https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops>
