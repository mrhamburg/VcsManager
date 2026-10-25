using VcsManager.Tests;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaProviderConformanceTests : VcsProviderConformanceTests
{
    protected override IVcsProvider CreateProvider()
    {
        return CreateProvider(new FakeGiteaApiClient());
    }

    protected override IVcsProvider CreateProviderWithMergeConflict()
    {
        return CreateProvider(new FakeGiteaApiClient { MergeConflict = true });
    }

    protected override IVcsProvider CreateProviderWithUnsupportedMergeMethod()
    {
        return CreateProvider(new FakeGiteaApiClient
        {
            UnsupportedMergeMethod = UnsupportedMergeMethod,
        });
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

    private static GiteaProvider CreateProvider(FakeGiteaApiClient client)
    {
        return new GiteaProvider(client, new GiteaOptions
        {
            BaseUrl = "https://gitea.example.com",
            Token = "test-token",
            DefaultOwner = "conformance",
        });
    }
}
