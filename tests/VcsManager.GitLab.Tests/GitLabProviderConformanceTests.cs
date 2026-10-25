using VcsManager.Tests;

namespace VcsManager.GitLab.Tests;

public sealed class GitLabProviderConformanceTests : VcsProviderConformanceTests
{
    protected override IVcsProvider CreateProvider()
    {
        return CreateProvider(new FakeGitLabApiClient());
    }

    protected override IVcsProvider CreateProviderWithMergeConflict()
    {
        return CreateProvider(new FakeGitLabApiClient { MergeConflict = true });
    }

    protected override IVcsProvider CreateProviderWithUnsupportedMergeMethod()
    {
        return CreateProvider(new FakeGitLabApiClient());
    }

    protected override RepositoryRef CreateRepositoryRef(string name)
    {
        return new RepositoryRef { Owner = "conformance", Name = name };
    }

    protected override CreateRepositoryRequest CreateRepositoryRequest(string name)
    {
        return new CreateRepositoryRequest
        {
            Owner = "conformance",
            Name = name,
            DefaultBranch = "main",
        };
    }

    private static GitLabProvider CreateProvider(FakeGitLabApiClient client)
    {
        return new GitLabProvider(client, new GitLabOptions { Token = "test-token" });
    }
}
