namespace VcsManager.AzureDevOps.Tests;

public sealed class AzureDevOpsOptionsTests
{
    [Fact]
    public void Validate_WithDevAzureComUrl_DerivesOrganizationName()
    {
        var options = new AzureDevOpsOptions
        {
            OrganizationUrl = "https://dev.azure.com/test-organization/",
            PersonalAccessToken = "test-token",
        };

        options.Validate();

        Assert.Equal("test-organization", options.GetOrganizationName());
        Assert.Equal(new Uri("https://dev.azure.com/test-organization/"), options.CreateOrganizationUri());
    }

    [Fact]
    public void Validate_WithVisualStudioUrl_DerivesOrganizationName()
    {
        var options = new AzureDevOpsOptions
        {
            OrganizationUrl = "https://test-organization.visualstudio.com",
            PersonalAccessToken = "test-token",
        };

        options.Validate();

        Assert.Equal("test-organization", options.GetOrganizationName());
    }

    [Fact]
    public void Validate_WithDevAzureComUrlWithoutOrganization_ThrowsArgumentException()
    {
        var options = new AzureDevOpsOptions
        {
            OrganizationUrl = "https://dev.azure.com",
            PersonalAccessToken = "test-token",
        };

        var exception = Assert.Throws<ArgumentException>(() => options.Validate());

        Assert.Contains("organization", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithNonHttpUrl_ThrowsArgumentException()
    {
        var options = new AzureDevOpsOptions
        {
            OrganizationUrl = "ftp://dev.azure.com/test-organization",
            PersonalAccessToken = "test-token",
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithEmptyApiVersion_ThrowsArgumentException()
    {
        var options = new AzureDevOpsOptions
        {
            OrganizationUrl = "https://dev.azure.com/test-organization",
            PersonalAccessToken = "test-token",
            ApiVersion = " ",
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Validate_DoesNotIncludeTokenInMessage()
    {
        var options = new AzureDevOpsOptions { PersonalAccessToken = "super-secret-token" };

        var exception = Assert.Throws<ArgumentException>(() => options.Validate());

        Assert.DoesNotContain("super-secret-token", exception.Message, StringComparison.Ordinal);
    }
}
