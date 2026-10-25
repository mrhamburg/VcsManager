# Integration tests

The integration tests run against live provider instances and are opt-in. They
are skipped when the required environment variables are absent.

## Gitea

Start a disposable Gitea instance with Docker:

```bash
docker run -d --name vcsmanager-gitea \
  -p 3000:3000 \
  -e GITEA__security__INSTALL_LOCK=true \
  gitea/gitea:latest
```

Open <http://localhost:3000>, create the first account, and create an access
token in **Settings > Applications** with repository read and write access.
Optionally create an organization and use it as the test owner.

```bash
export VCS_GITEA_TEST_URL=http://localhost:3000
export VCS_GITEA_TEST_TOKEN=<token>
export VCS_GITEA_TEST_ORG=<organization>   # optional
dotnet test tests/VcsManager.IntegrationTests
```

The tests create a uniquely named private repository and delete it in a
`finally` block. They never print the token. Without the environment variables,
the Gitea tests are reported as skipped.

Stop and remove the container:

```bash
docker rm -f vcsmanager-gitea
```
