using VcsManager.Tests;

namespace VcsManager.AzureDevOps.Tests;

public sealed class AzureDevOpsProviderConformanceTests : VcsProviderConformanceTests
{
    protected override IVcsProvider CreateProvider()
    {
        return CreateProvider(new FakeAzureDevOpsApiClient());
    }

    protected override IVcsProvider CreateProviderWithMergeConflict()
    {
        return CreateProvider(new FakeAzureDevOpsApiClient { MergeConflict = true });
    }

    protected override IVcsProvider CreateProviderWithUnsupportedMergeMethod()
    {
        return CreateProvider(new FakeAzureDevOpsApiClient
        {
            UnsupportedMergeMethod = UnsupportedMergeMethod,
        });
    }

    protected override RepositoryRef CreateRepositoryRef(string name)
    {
        return new RepositoryRef
        {
            Owner = FakeAzureDevOpsApiClient.Organization,
            Project = "conformance-project",
            Name = name,
        };
    }

    protected override CreateRepositoryRequest CreateRepositoryRequest(string name)
    {
        return new CreateRepositoryRequest
        {
            Owner = FakeAzureDevOpsApiClient.Organization,
            Project = "conformance-project",
            Name = name,
        };
    }

    private static AzureDevOpsProvider CreateProvider(FakeAzureDevOpsApiClient client)
    {
        var options = new AzureDevOpsOptions
        {
            OrganizationUrl = $"https://dev.azure.com/{FakeAzureDevOpsApiClient.Organization}",
            PersonalAccessToken = "test-token",
            DefaultProject = "conformance-project",
        };

        return new AzureDevOpsProvider(client, options);
    }
}
