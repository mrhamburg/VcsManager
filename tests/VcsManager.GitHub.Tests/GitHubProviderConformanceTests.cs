using VcsManager.Tests;

namespace VcsManager.GitHub.Tests;

public sealed class GitHubProviderConformanceTests : VcsProviderConformanceTests
{
    protected override IVcsProvider CreateProvider()
    {
        return CreateProvider(new FakeGitHubApiClient());
    }

    protected override IVcsProvider CreateProviderWithMergeConflict()
    {
        return CreateProvider(new FakeGitHubApiClient { MergeConflict = true });
    }

    protected override IVcsProvider CreateProviderWithUnsupportedMergeMethod()
    {
        return CreateProvider(new FakeGitHubApiClient { UnsupportedMergeMethod = MergeMethod.Rebase });
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

    private static GitHubProvider CreateProvider(FakeGitHubApiClient client)
    {
        return new GitHubProvider(client, new GitHubOptions { Token = "test-token" });
    }
}
