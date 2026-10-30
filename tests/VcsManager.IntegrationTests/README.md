# Integration tests

The integration tests are a console runner. The runner tests the full
`IVcsProvider` interface against a live provider:

1. Create a private repository.
2. Get, check, and list repositories.
3. Create, get, list, merge, and close pull requests.
4. Close a pull request a second time to verify idempotency.
5. Delete the repository and verify that it is gone.

The runner creates the initial repository commit with `SeedRepositoryAsync`.
It still uses a test-only helper to create the branches that the pull request
steps need. The branch helper is not part of the public API. The runner never
prints tokens.

Each provider has a YAML sample in `configs/`. The config uses the same format
as `VcsProviderBuilder.FromYamlFile`.

## Run locally

Build the runner and pass a config path:

```bash
dotnet run --project tests/VcsManager.IntegrationTests \
  -- tests/VcsManager.IntegrationTests/configs/gitea.yaml
```

The runner returns `0` when all steps pass and `1` when a step fails.

## Gitea

Start a disposable Gitea instance:

```bash
docker run -d --name vcsmanager-gitea \
  -p 3000:3000 \
  -e GITEA__security__INSTALL_LOCK=true \
  gitea/gitea:latest
```

Open <http://localhost:3000>, create the first account, and create an access
token in **Settings > Applications**. Select all scopes, or at least the
repository and user write scopes.

Copy `configs/gitea.yaml` to a local file and fill in the token and owner:

```yaml
provider:
  gitea:
    base:
      url: http://localhost:3000
    token: <token>
    default:
      owner: <user-or-organization>
```

Then run:

```bash
dotnet run --project tests/VcsManager.IntegrationTests -- my-gitea.yaml
```

Stop and remove the container:

```bash
docker rm -f vcsmanager-gitea
```

## GitHub

Use a token with the `repo` and `delete_repo` scopes. Set `base.url` for GitHub
Enterprise Server. Set `default.owner` to create the test repository in an
organization. Without `default.owner`, the authenticated user owns the
repository.

## GitLab

Use a token with the `api` scope. Set `default.namespace` to the group that
receives the test repository. For a self-hosted instance, set `base.url` to
the server URL.

## Azure DevOps

The team project must already exist. Set `default.project` to that project.
Use a personal access token with permission to create and delete repositories
in the project. Azure DevOps creates an empty repository, so the runner seeds
the default branch through `SeedRepositoryAsync` before it creates the pull
request branches.

## CI

CI runs the Gitea config on every push to `main`. GitHub, GitLab, and Azure
DevOps jobs are present in the workflow but are off. Set the matching
`HAS_*_SECRETS` repository variable and the provider secrets to enable one.

The latest status for each provider is shown in the project `README.md` and on
each provider page. The badges read the GitHub check run for the provider's
integration job, so a provider that is not configured shows a grey
`no check runs` badge instead of a result.
