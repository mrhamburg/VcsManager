# Samples

The sample console app runs the full repository and pull request flow with the
in-memory `demo` provider:

```bash
cd samples/VcsManager.Samples
VCS_PROVIDER=demo dotnet run
```

For a GitHub, GitLab, Azure DevOps, or Gitea version of the same flow,
including configuration, token scopes, merge behavior, and deletion warnings,
see [`docs/providers/github.md`](../docs/providers/github.md),
[`docs/providers/gitlab.md`](../docs/providers/gitlab.md),
[`docs/providers/azure-devops.md`](../docs/providers/azure-devops.md), or
[`docs/providers/gitea.md`](../docs/providers/gitea.md).
