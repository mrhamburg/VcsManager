using System.Net;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Exceptions;
using NSubstitute;
using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps.Tests;

public sealed class AzureDevOpsRepositoryTests
{
    [Fact]
    public async Task CreateRepository_WithRequestProject_PassesProjectAndMapsResult()
    {
        var (provider, client) = CreateProvider();
        var request = new CreateRepositoryRequest
        {
            Owner = TestData.Organization,
            Project = TestData.Project,
            Name = "test-repository",
        };

        client.CreateRepositoryAsync(
                TestData.Project,
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.Repository());

        var repository = await provider.CreateRepositoryAsync(request);

        Assert.Equal("11111111-1111-1111-1111-111111111111", repository.Id);
        Assert.Equal("test-repository", repository.Name);
        Assert.Equal("test-organization", repository.Owner);
        Assert.Equal("test-project", repository.Project);
        Assert.Equal("test-organization/test-project/test-repository", repository.FullName);
        Assert.Equal(
            "https://dev.azure.com/test-organization/test-project/_git/test-repository",
            repository.Url);
        Assert.Equal("main", repository.DefaultBranch);
        Assert.Equal(VcsVisibility.Private, repository.Visibility);
        await client.Received(1).CreateRepositoryAsync(
            TestData.Project,
            request,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRepository_WithDefaultProject_UsesConfiguredProject()
    {
        var (provider, client) = CreateProvider();
        var request = new CreateRepositoryRequest
        {
            Owner = TestData.Organization,
            Name = "test-repository",
        };

        client.CreateRepositoryAsync(
                TestData.Project,
                request,
                Arg.Any<CancellationToken>())
            .Returns(TestData.Repository());

        var repository = await provider.CreateRepositoryAsync(request);

        Assert.Equal(TestData.Project, repository.Project);
    }

    [Fact]
    public async Task CreateRepository_WithoutProject_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider(defaultProject: null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.CreateRepositoryAsync(new CreateRepositoryRequest
            {
                Owner = TestData.Organization,
                Name = "test-repository",
            }));

        Assert.Contains("project", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRepository_WithMissingName_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.CreateRepositoryAsync(
            new CreateRepositoryRequest
            {
                Owner = TestData.Organization,
                Project = TestData.Project,
                Name = " ",
            }));
    }

    [Fact]
    public async Task CreateRepository_WithInitializeWithReadme_ThrowsUnsupportedOperation()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<VcsUnsupportedOperationException>(
            () => provider.CreateRepositoryAsync(new CreateRepositoryRequest
            {
                Owner = TestData.Organization,
                Project = TestData.Project,
                Name = "test-repository",
                InitializeWithReadme = true,
            }));
    }

    [Fact]
    public async Task CreateRepository_WithMismatchedOwner_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.CreateRepositoryAsync(new CreateRepositoryRequest
            {
                Owner = "other-organization",
                Project = TestData.Project,
                Name = "test-repository",
            }));

        Assert.Contains("owner", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRepository_Duplicate_ThrowsVcsConflictException()
    {
        var (provider, client) = CreateProvider();

        client.CreateRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<CreateRepositoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<AzureDevOpsRepository>>(_ => throw CreateResponseException(
                HttpStatusCode.Conflict,
                "A repository with this name already exists."));

        await Assert.ThrowsAsync<VcsConflictException>(() => provider.CreateRepositoryAsync(
            new CreateRepositoryRequest
            {
                Owner = TestData.Organization,
                Project = TestData.Project,
                Name = "test-repository",
            }));
    }

    [Fact]
    public async Task GetRepository_UsesConfiguredProjectAndMapsResult()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                TestData.Project,
                "test-repository",
                Arg.Any<CancellationToken>())
            .Returns(TestData.Repository());

        var repository = await provider.GetRepositoryAsync(
            new RepositoryRef { Name = "test-repository" });

        Assert.Equal("test-organization/test-project/test-repository", repository.FullName);
    }

    [Fact]
    public async Task GetRepository_Missing_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<AzureDevOpsRepository>>(_ => throw CreateResponseException(
                HttpStatusCode.NotFound,
                "Not Found"));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.GetRepositoryAsync(new RepositoryRef
            {
                Owner = TestData.Organization,
                Project = TestData.Project,
                Name = "missing",
            }));
    }

    [Fact]
    public async Task GetRepository_WithMismatchedOwner_ThrowsArgumentException()
    {
        var (provider, _) = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.GetRepositoryAsync(new RepositoryRef
            {
                Owner = "other-organization",
                Project = TestData.Project,
                Name = "test-repository",
            }));
    }

    [Fact]
    public async Task RepositoryExists_WhenFound_ReturnsTrue()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                TestData.Project,
                "test-repository",
                Arg.Any<CancellationToken>())
            .Returns(TestData.Repository());

        Assert.True(await provider.RepositoryExistsAsync(
            new RepositoryRef { Name = "test-repository" }));
    }

    [Fact]
    public async Task RepositoryExists_WhenMissing_ReturnsFalse()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<AzureDevOpsRepository>>(_ => throw CreateResponseException(
                HttpStatusCode.NotFound,
                "Not Found"));

        Assert.False(await provider.RepositoryExistsAsync(
            new RepositoryRef { Name = "missing" }));
    }

    [Fact]
    public async Task RepositoryExists_WhenUnauthorized_ThrowsAuthenticationException()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<AzureDevOpsRepository>>(_ => throw CreateResponseException(
                HttpStatusCode.Unauthorized,
                "Unauthorized"));

        await Assert.ThrowsAsync<VcsAuthenticationException>(
            () => provider.RepositoryExistsAsync(
                new RepositoryRef { Name = "test-repository" }));
    }

    [Fact]
    public async Task DeleteRepository_DeletesRepository()
    {
        var (provider, client) = CreateProvider();

        await provider.DeleteRepositoryAsync(new RepositoryRef
        {
            Owner = TestData.Organization,
            Project = TestData.Project,
            Name = "test-repository",
        });

        await client.Received(1).DeleteRepositoryAsync(
            TestData.Project,
            "test-repository",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRepository_Missing_ThrowsRepositoryNotFoundException()
    {
        var (provider, client) = CreateProvider();

        client.DeleteRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw CreateResponseException(
                HttpStatusCode.NotFound,
                "Not Found"));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => provider.DeleteRepositoryAsync(new RepositoryRef
            {
                Owner = TestData.Organization,
                Project = TestData.Project,
                Name = "missing",
            }));
    }

    [Fact]
    public async Task RepositoryOperation_WhenForbidden_ThrowsAuthorizationException()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<AzureDevOpsRepository>>(_ => throw CreateResponseException(
                HttpStatusCode.Forbidden,
                "Forbidden"));

        await Assert.ThrowsAsync<VcsAuthorizationException>(
            () => provider.GetRepositoryAsync(
                new RepositoryRef { Name = "test-repository" }));
    }

    [Fact]
    public async Task RepositoryOperation_WhenRateLimited_ThrowsRateLimitException()
    {
        var (provider, client) = CreateProvider();

        client.GetRepositoryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<AzureDevOpsRepository>>(_ => throw new TooManyRequestsException(
                "Too many requests.",
                DateTime.UtcNow.AddSeconds(30)));

        var exception = await Assert.ThrowsAsync<VcsRateLimitException>(
            () => provider.GetRepositoryAsync(
                new RepositoryRef { Name = "test-repository" }));

        Assert.NotNull(exception.RetryAfter);
        Assert.True(exception.RetryAfter > TimeSpan.Zero);
        Assert.True(exception.RetryAfter <= TimeSpan.FromSeconds(31));
    }

    internal static VssServiceResponseException CreateResponseException(
        HttpStatusCode statusCode,
        string content)
    {
        return new VssServiceResponseException(
            statusCode,
            $"Azure DevOps returned HTTP {(int)statusCode}.",
            content,
            innerException: null);
    }

    internal static (AzureDevOpsProvider Provider, IAzureDevOpsApiClient Client) CreateProvider(
        string? defaultProject = TestData.Project)
    {
        var options = new AzureDevOpsOptions
        {
            OrganizationUrl = $"https://dev.azure.com/{TestData.Organization}",
            PersonalAccessToken = "test-token",
            DefaultProject = defaultProject,
        };

        var client = Substitute.For<IAzureDevOpsApiClient>();
        return (new AzureDevOpsProvider(client, options), client);
    }
}
