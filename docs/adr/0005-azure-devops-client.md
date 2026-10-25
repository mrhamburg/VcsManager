# ADR-0005: Azure DevOps uses the official SDK with REST 7.1 fallback

- Status: Accepted
- Date: 2026-10-25

## Context

Azure DevOps repository and pull request operations are available through the
official .NET client libraries and through the REST API. The SDK provides typed
clients and authentication, but some newer request shapes are easier or only
available through REST. Mixing both approaches without a rule would create two
error models and two pagination styles.

## Decision

The `VcsManager.AzureDevOps` package uses
**`Microsoft.TeamFoundationServer.Client` 20.256.2** as the primary client for
repository and pull request operations.

- The stable `20.256.2` package targets .NET Standard 2.0 and is usable from
  `net10.0`. It is pinned to that exact version.
- The SDK `GitHttpClient` is wrapped in an internal `IAzureDevOpsApiClient`
  interface. Unit tests replace this interface, not Azure DevOps itself.
- **REST API version 7.1** is the fallback for an operation that the SDK does
  not expose or cannot express. Each fallback is documented next to its client
  method and covered by tests.
- Authentication uses `VssBasicCredential` with an empty user name and the PAT
  as the password, matching the v1 authentication decision (ADR-0002).
- The SDK `GitPullRequest` model is converted to core records inside the
  provider. No SDK type appears in the public API.

This decision is final for v1. Changing the primary client requires a new ADR.

### Reviewer votes and completion

Azure DevOps reviewer votes and pull request completion are different
operations:

- A reviewer **vote** (`10` approved, `5` approved with suggestions, `0` no
  vote, `-5` waiting for author, `-10` rejected) records review state only. It
  does not merge or complete a pull request.
- **Completion** sets `status = completed`, supplies
  `lastMergeSourceCommit`, and sets `GitPullRequestCompletionOptions`
  (including `mergeStrategy`). This is the operation that maps to
  `MergeMethod` in ADR-0007.
- `MergePullRequestRequest` has no reviewer or approval field. v1 does not add
  or inspect reviewer votes.

## Consequences

- The SDK handles Azure DevOps authentication, retries, and typed responses.
- The REST fallback keeps the provider viable when the SDK lags the service.
- The SDK brings Newtonsoft.Json and Microsoft Visual Studio Services
  dependencies into the provider package. They stay out of the core package.
- Provider tests must cover both the SDK path and each REST fallback path.
- Rate-limit and error responses are normalized to the core error contract
  (ADR-0009) regardless of which path produced them.

## References

- Microsoft.TeamFoundationServer.Client 20.256.2: <https://www.nuget.org/packages/Microsoft.TeamFoundationServer.Client/20.256.2>
- Azure DevOps REST API 7.1: <https://learn.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-7.1>
- Azure DevOps repositories API: <https://learn.microsoft.com/en-us/rest/api/azure/devops/git/repositories?view=azure-devops-rest-7.1>
- Azure DevOps pull requests API: <https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests?view=azure-devops-rest-7.1>
- Azure DevOps pull request update API: <https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests/update?view=azure-devops-rest-7.1>
- Azure DevOps .NET samples: <https://github.com/microsoft/azure-devops-dotnet-samples>
