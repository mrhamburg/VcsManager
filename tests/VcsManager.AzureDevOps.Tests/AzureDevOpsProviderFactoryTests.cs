using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps.Tests;

public sealed class AzureDevOpsProviderFactoryTests
{
    [Fact]
    public void ProviderName_IsAzureDevOps()
    {
        var factory = new AzureDevOpsProviderFactory();

        Assert.Equal("azuredevops", factory.ProviderName);
    }

    [Fact]
    public void MapConfiguration_WithFullSettings_MapsAllValues()
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["organization.url"] = "https://dev.azure.com/test-organization",
            ["personal.access.token"] = "test-token",
            ["default.project"] = "test-project",
            ["api.version"] = "7.0",
        };

        var options = AzureDevOpsProviderFactory.MapConfiguration(configuration);

        Assert.Equal("https://dev.azure.com/test-organization", options.OrganizationUrl);
        Assert.Equal("test-token", options.PersonalAccessToken);
        Assert.Equal("test-project", options.DefaultProject);
        Assert.Equal("7.0", options.ApiVersion);
    }

    [Fact]
    public void MapConfiguration_WithPartialSettings_UsesDefaults()
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["organization.url"] = "https://dev.azure.com/test-organization",
            ["personal.access.token"] = "test-token",
        };

        var options = AzureDevOpsProviderFactory.MapConfiguration(configuration);

        Assert.Null(options.DefaultProject);
        Assert.Equal(AzureDevOpsOptions.DefaultApiVersion, options.ApiVersion);
    }

    [Fact]
    public void MapConfiguration_WithEmptySettings_UsesDefaults()
    {
        var options = AzureDevOpsProviderFactory.MapConfiguration(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        Assert.Null(options.OrganizationUrl);
        Assert.Null(options.PersonalAccessToken);
        Assert.Null(options.DefaultProject);
        Assert.Equal(AzureDevOpsOptions.DefaultApiVersion, options.ApiVersion);
    }

    [Fact]
    public void MapConfiguration_IgnoresWhitespaceValues()
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["organization.url"] = "  ",
            ["personal.access.token"] = "  ",
            ["default.project"] = "  ",
            ["api.version"] = "  ",
        };

        var options = AzureDevOpsProviderFactory.MapConfiguration(configuration);

        Assert.Null(options.OrganizationUrl);
        Assert.Null(options.PersonalAccessToken);
        Assert.Null(options.DefaultProject);
        Assert.Equal(AzureDevOpsOptions.DefaultApiVersion, options.ApiVersion);
    }

    [Fact]
    public void Create_WithoutOrganizationUrl_ThrowsClearArgumentException()
    {
        var factory = new AzureDevOpsProviderFactory();

        var exception = Assert.Throws<ArgumentException>(() => factory.Create(
            new Dictionary<string, string> { ["personal.access.token"] = "test-token" }));

        Assert.Contains("organization", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithoutToken_ThrowsClearArgumentException()
    {
        var factory = new AzureDevOpsProviderFactory();

        var exception = Assert.Throws<ArgumentException>(() => factory.Create(
            new Dictionary<string, string>
            {
                ["organization.url"] = "https://dev.azure.com/test-organization",
            }));

        Assert.Contains("token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithSettings_ReturnsAzureDevOpsProvider()
    {
        var factory = new AzureDevOpsProviderFactory();

        var provider = factory.Create(CreateConfiguration());

        Assert.IsType<AzureDevOpsProvider>(provider);
    }

    [Fact]
    public void ApiClient_UsesOrganizationBaseAddress()
    {
        var client = new AzureDevOpsApiClient(CreateOptions());

        Assert.Equal(
            new Uri("https://dev.azure.com/test-organization/"),
            client.BaseAddress);

        Assert.Equal("test-organization", client.Options.GetOrganizationName());
    }

    internal static Dictionary<string, string> CreateConfiguration()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["organization.url"] = "https://dev.azure.com/test-organization",
            ["personal.access.token"] = "test-token",
            ["default.project"] = "test-project",
        };
    }

    internal static AzureDevOpsOptions CreateOptions()
    {
        return new AzureDevOpsOptions
        {
            OrganizationUrl = "https://dev.azure.com/test-organization",
            PersonalAccessToken = "test-token",
            DefaultProject = "test-project",
        };
    }
}
