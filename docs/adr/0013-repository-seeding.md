# ADR-0013: Repository seeding from a template

- Status: Accepted
- Date: 2026-10-25

## Context

The integration runner creates a repository and then uses the test-only
`ProviderBranchSeeder` to write files through provider-specific REST calls. That
helper is not part of the public API, so callers cannot bootstrap a repository
from an existing folder template. Each caller must instead use provider-specific
code to create an initial commit.

The providers do not share a native "create from template" feature:

- GitHub can generate a repository from a remote template repository.
- Gitea can generate a repository from a remote template repository.
- GitLab can create a project from a built-in or group template.
- Azure DevOps has no equivalent template generation endpoint.

Those features require the template to exist on the provider. They do not accept
a local folder, and they are not portable. VcsManager needs one operation that
seeds an empty repository from a set of files on all four providers.

## Decision

Add a first-class repository seeding operation to `IVcsProvider`:

```csharp
Task SeedRepositoryAsync(
    RepositoryRef repository,
    SeedRepositoryRequest request,
    CancellationToken cancellationToken = default);
```

`SeedRepositoryRequest` carries:

```csharp
public record SeedRepositoryRequest
{
    public required string Branch { get; init; }
    public required IReadOnlyList<RepositorySeedFile> Files { get; init; }
    public string? CommitMessage { get; init; }
}

public record RepositorySeedFile
{
    public required string Path { get; init; }
    public required ReadOnlyMemory<byte> Content { get; init; }
}
```

### Operation semantics

- The operation writes exactly one commit that contains every supplied file.
  Providers must not split a seed into multiple commits. If a provider cannot
  create the commit atomically, it returns `VcsUnsupportedOperationException`
  instead of partially seeding the repository.
- The commit is the initial commit of the repository. The target repository must
  have no commits. If the repository already has a commit, or if `Branch`
  already exists, the operation returns `VcsConflictException`.
- `Branch` is required. Empty repositories do not have a reliable default branch
  across providers, so the caller must choose the branch that receives the
  template. After the commit, `Branch` is the repository default branch. The
  provider sets the default branch explicitly when the provider does not do so
  automatically.
- `CommitMessage` defaults to `Initial commit`. The authenticated token identity
  is the author and committer. Author and committer overrides are not part of
  v1.
- The caller resolves the existing folder template into `Files` before calling
  the operation. The core library does not read the filesystem. This keeps the
  operation usable from any host and prevents filesystem I/O from running inside
  provider layers, retries, and timeouts. The intended caller pattern is:

```csharp
var files = Directory
    .EnumerateFiles(templateDirectory, "*", SearchOption.AllDirectories)
    .Select(path => new RepositorySeedFile
    {
        Path = Path.GetRelativePath(templateDirectory, path).Replace('\\', '/'),
        Content = File.ReadAllBytes(path),
    })
    .ToArray();

await provider.SeedRepositoryAsync(
    repository,
    new SeedRepositoryRequest { Branch = "main", Files = files });
```

### File rules

- `Files` must contain at least one entry.
- `Path` is relative to the repository root and uses `/` separators. A path must
  not start with `/`, must not contain empty, `.` or `..` segments, and must not
  contain a `.git` segment.
- Duplicate paths are invalid.
- `Content` is binary. Zero-length files are allowed. Providers encode the
  content as the provider API requires.
- Empty directories are not represented. A folder template that relies on empty
  directories loses them.
- File modes, executable bits, symlinks, submodules, and Git LFS pointers are not
  portable across the four providers and are not part of v1. Callers must supply
  only regular files. A `.gitignore`, `.gitattributes`, or `.editorconfig` file is
  copied as ordinary content and does not change how the seed is written.
- The operation creates the commit in one logical operation. It does not chunk
  large templates. Provider file-count and request-size limits apply and map to
  the normal error contract.

### Validation and errors

- A null repository or request, an empty file list, an invalid path, or an
  invalid branch returns `ArgumentException` before any provider call when the
  value can be validated without a provider request.
- A missing repository returns `RepositoryNotFoundException`.
- Missing or invalid credentials return `VcsAuthenticationException`; missing
  permission returns `VcsAuthorizationException`.
- A non-empty repository or an existing target branch returns
  `VcsConflictException`.
- A rate limit returns `VcsRateLimitException`.
- A provider that cannot perform the operation returns
  `VcsUnsupportedOperationException`.
- Other provider failures return `VcsProviderException`.
- Error messages include the provider, operation, and repository address. They
  never include file contents, tokens, authorization headers, or request bodies.

### Provider mapping

| Provider | Initial commit mechanism | Conditions |
| --- | --- | --- |
| GitHub | Git data API: create blobs, create a tree, create a commit, then create the branch ref. Update the default branch when required. | The token needs contents write access (`repo` for classic PATs). The repository must be empty. |
| GitLab | Commits API with one `actions` entry per file and base64 content. Update the project default branch when required. | The token needs `api` or `write_repository`. The project must be empty. |
| Azure DevOps | Pushes API with one commit and `add` changes. Use an all-zero `oldObjectId` for the initial ref update. Set the default branch when required. | The token needs `vso.code_write`. The team project must exist. The repository must be empty. |
| Gitea | Multi-file contents API with one `create` operation per file and base64 content. | The token needs repository write access. The repository must be empty. Gitea sets the default branch on the first commit. |

### Layers and integration

- `VcsProviderDecorator` forwards `SeedRepositoryAsync`.
- The logging, metrics, trace, timeout, and concurrency layers wrap it as they
  wrap other operations.
- `RetryLayer` must not retry seeding automatically. Seeding is not idempotent;
  a retry can turn a successful but interrupted seed into a conflict. This is the
  same rule that already applies to create, delete, and merge operations.
- The integration runner can create the repository with
  `InitializeWithReadme = false` and use `SeedRepositoryAsync` for its initial
  commit. The test-only branch seeder remains for pull request branches until a
  separate branch-and-commit capability is designed.
- The capability matrix gains a `Seed repository` row when the operation ships.

## Alternatives considered

- **Add template files to `CreateRepositoryRequest`.** Rejected because it hides
  a second operation behind repository creation, cannot seed a repository created
  by another tool, and makes the failure boundary unclear when creation succeeds
  but seeding fails.
- **Use provider-native template repositories.** Rejected because GitHub, GitLab,
  and Gitea require a remote template repository, Azure DevOps has no equivalent,
  and the features do not accept a local folder.
- **Clone the template and push with Git.** Rejected because it adds a Git
  runtime and credential-handling responsibility to a provider-API library.
- **Accept a local folder path in the core request.** Rejected because it puts
  filesystem I/O into the provider pipeline, makes retries and timeouts re-read
  the template, and complicates hosts without a local filesystem.
- **Allow seeding into a non-empty repository.** Rejected because it can
  overwrite existing files and changes the operation from safe initialization to
  a general file update. A future ADR can define a separate commit operation.

## Consequences

- `IVcsProvider` gains a member. This is a breaking change for custom provider
  implementations, which is acceptable before v1.
- Every provider and every layer must implement or forward the new operation.
  Contract tests, provider tests, and the integration runner must cover it.
- Callers that want an initial template must create the repository with
  `InitializeWithReadme = false`, then call `SeedRepositoryAsync`.
- The capability is supported by all four providers, with the conditions in the
  provider mapping table.
- The operation is not idempotent. A second call against the same repository
  returns `VcsConflictException`.
- No new provider SDK or dependency is required.

## References

- GitHub Git data API: [https://docs.github.com/en/rest/git/blobs](https://docs.github.com/en/rest/git/blobs)
- GitHub tree API: [https://docs.github.com/en/rest/git/trees](https://docs.github.com/en/rest/git/trees)
- GitHub commit API: [https://docs.github.com/en/rest/git/commits](https://docs.github.com/en/rest/git/commits)
- GitHub reference API: [https://docs.github.com/en/rest/git/refs](https://docs.github.com/en/rest/git/refs)
- GitLab commits API: [https://docs.gitlab.com/api/commits/#create-a-commit-with-multiple-files-and-actions](https://docs.gitlab.com/api/commits/#create-a-commit-with-multiple-files-and-actions)
- GitLab projects API: [https://docs.gitlab.com/api/projects/](https://docs.gitlab.com/api/projects/)
- Azure DevOps pushes API: [https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pushes/create?view=azure-devops-rest-7.1](https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pushes/create?view=azure-devops-rest-7.1)
- Azure DevOps repositories API: [https://learn.microsoft.com/en-us/rest/api/azure/devops/git/repositories?view=azure-devops-rest-7.1](https://learn.microsoft.com/en-us/rest/api/azure/devops/git/repositories?view=azure-devops-rest-7.1)
- Gitea API documentation: [https://docs.gitea.com/api/1.0/](https://docs.gitea.com/api/1.0/)
- Gitea Swagger document: [https://gitea.com/swagger.v1.json](https://gitea.com/swagger.v1.json)
