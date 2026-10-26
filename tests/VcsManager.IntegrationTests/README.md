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

The GitLab flow supports a disposable GitLab CE instance through Docker
Compose, or an existing instance through environment variables.

### Disposable GitLab CE instance

Run the script. It starts GitLab CE, waits until it is ready, creates a private
group and an access token, runs the full flow, and removes the container and
its volumes:

```bash
tests/VcsManager.IntegrationTests/run-gitlab-integration.sh
```

The first start can take several minutes. The container uses port `8929` by
default. Override it with `VCS_GITLAB_TEST_PORT`. The script skips cleanly when
Docker is not installed or not running.

### Existing GitLab instance

Set all three variables and run the runner in GitLab mode:

```bash
export VCS_GITLAB_TEST_URL=http://localhost:8929
export VCS_GITLAB_TEST_TOKEN=<token>
export VCS_GITLAB_TEST_NAMESPACE=<group>

dotnet run --project tests/VcsManager.IntegrationTests -- --gitlab
```

The token needs the `api` scope. The namespace must be a group, because the
runner lists the project through the group scope. The runner prints a skip
message and returns `0` when any variable is missing.

You can also use a YAML config with `default.namespace` set to the group that
receives the test repository. For a self-hosted instance, set `base.url` to the
server URL.

### Manual Compose control

To keep the instance between runs:

```bash
VCS_GITLAB_TEST_PORT=8929 \
  docker compose -f tests/VcsManager.IntegrationTests/docker-compose.gitlab.yml up -d
```

Provision the group and token as the script does, or create them in the GitLab
UI. Stop and remove the instance and volumes when done:

```bash
docker compose -f tests/VcsManager.IntegrationTests/docker-compose.gitlab.yml down -v
```

## Azure DevOps

The team project must already exist. Set `default.project` to that project.
Use a personal access token with permission to create and delete repositories
in the project. Azure DevOps creates an empty repository, so the runner seeds
the default branch through `SeedRepositoryAsync` before it creates the pull
request branches.

## CI

CI runs the Gitea config and the disposable GitLab CE flow. GitHub and Azure
DevOps jobs are present in the workflow but are off. Set the matching
`HAS_*_SECRETS` repository variable and the provider secrets to enable one.
