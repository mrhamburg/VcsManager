using VcsManager.Builder;

namespace VcsManager.AzureDevOps.Tests;

public sealed class VcsProviderBuilderExtensionsTests
{
    [Fact]
    public void WithAzureDevOps_RegistersProviderAndBuilds()
    {
        var builder = new VcsProviderBuilder()
            .WithAzureDevOps(options =>
            {
                options.OrganizationUrl = "https://dev.azure.com/test-organization";
                options.PersonalAccessToken = "test-token";
                options.DefaultProject = "test-project";
            });

        var provider = builder.Build();

        Assert.IsType<AzureDevOpsProvider>(provider);
    }

    [Fact]
    public void WithAzureDevOps_WithoutToken_ThrowsArgumentException()
    {
        var builder = new VcsProviderBuilder()
            .WithAzureDevOps(options =>
                options.OrganizationUrl = "https://dev.azure.com/test-organization");

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());

        Assert.Contains("token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithAzureDevOps_WithoutConfiguration_RequiresProviderSelection()
    {
        var builder = new VcsProviderBuilder().WithAzureDevOps();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("No provider configured", exception.Message);
    }

    [Fact]
    public void WithAzureDevOps_CanBeCombinedWithEnvironmentConfiguration()
    {
        const string prefix = "VCSM_TEST_AZURE";
        Environment.SetEnvironmentVariable($"{prefix}_PROVIDER", "azuredevops");
        Environment.SetEnvironmentVariable(
            $"{prefix}_AZUREDEVOPS_ORGANIZATION_URL",
            "https://dev.azure.com/test-organization");
        Environment.SetEnvironmentVariable(
            $"{prefix}_AZUREDEVOPS_PERSONAL_ACCESS_TOKEN",
            "test-token");

        try
        {
            var builder = new VcsProviderBuilder()
                .WithAzureDevOps()
                .FromEnvironment(prefix);

            var provider = builder.Build();

            Assert.IsType<AzureDevOpsProvider>(provider);
        }
        finally
        {
            Environment.SetEnvironmentVariable($"{prefix}_PROVIDER", null);
            Environment.SetEnvironmentVariable($"{prefix}_AZUREDEVOPS_ORGANIZATION_URL", null);
            Environment.SetEnvironmentVariable($"{prefix}_AZUREDEVOPS_PERSONAL_ACCESS_TOKEN", null);
        }
    }
}
