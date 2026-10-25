using System.Net;
using System.Text;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using VcsManager.AzureDevOps.Internal;

namespace VcsManager.AzureDevOps.Tests;

public sealed class AzureDevOpsApiClientTests
{
    private const string RepositoryJson =
        """
        {
          "count": 1,
          "value": [
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "name": "test-repository",
              "webUrl": "https://dev.azure.com/test-organization/test-project/_git/test-repository",
              "defaultBranch": "refs/heads/main",
              "project": { "id": "33333333-3333-3333-3333-333333333333", "name": "test-project" }
            }
          ]
        }
        """;

    [Fact]
    public async Task ListRepositories_WithoutProject_UsesOrganizationEndpoint()
    {
        var handler = new StubHttpMessageHandler(RepositoryJson);
        await using var client = CreateClient(handler);

        var page = await client.ListRepositoriesAsync(
            project: null,
            new RepositoryQuery(),
            new PaginationOptions { Page = 2, PageSize = 3 },
            CancellationToken.None);

        Assert.Single(page.Items);
        Assert.False(page.HasNextPage);
        Assert.Null(page.TotalCount);
        Assert.Equal("test-project", page.Items[0].Project);
        Assert.Equal("test-organization", page.Items[0].Organization);

        var uri = Assert.IsType<Uri>(handler.RequestUri);
        Assert.Equal(
            $"/{TestData.Organization}/_apis/git/repositories",
            uri.AbsolutePath);
        Assert.Contains("api-version=7.1", uri.Query, StringComparison.Ordinal);
        Assert.Contains("%24top=4", uri.Query, StringComparison.Ordinal);
        Assert.Contains("%24skip=3", uri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListRepositories_WithProject_UsesProjectEndpoint()
    {
        var handler = new StubHttpMessageHandler(RepositoryJson);
        await using var client = CreateClient(handler);

        await client.ListRepositoriesAsync(
            TestData.Project,
            new RepositoryQuery(),
            new PaginationOptions(),
            CancellationToken.None);

        var uri = Assert.IsType<Uri>(handler.RequestUri);
        Assert.Equal(
            $"/{TestData.Organization}/{TestData.Project}/_apis/git/repositories",
            uri.AbsolutePath);
    }

    [Fact]
    public async Task ListRepositories_WhenMoreItemsExist_ReportsNextPage()
    {
        var items = string.Join(
            ",",
            Enumerable.Range(1, 4).Select(index =>
                $$"""
                {
                  "id": "11111111-1111-1111-1111-11111111111{{index}}",
                  "name": "repository-{{index}}",
                  "webUrl": "https://dev.azure.com/test-organization/test-project/_git/repository-{{index}}",
                  "project": { "id": "33333333-3333-3333-3333-333333333333", "name": "test-project" }
                }
                """));

        var handler = new StubHttpMessageHandler($$"""{"count": 4, "value": [{{items}}]}""");
        await using var client = CreateClient(handler);

        var page = await client.ListRepositoriesAsync(
            project: null,
            new RepositoryQuery(),
            new PaginationOptions { PageSize = 3 },
            CancellationToken.None);

        Assert.Equal(3, page.Items.Count);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task ListRepositories_WhenRestCallFails_ThrowsApiException()
    {
        var handler = new StubHttpMessageHandler(
            "{\"message\":\"Unauthorized\"}",
            HttpStatusCode.Unauthorized);
        await using var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AzureDevOpsApiException>(
            () => client.ListRepositoriesAsync(
                project: null,
                new RepositoryQuery(),
                new PaginationOptions(),
                CancellationToken.None));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Contains("Unauthorized", exception.ResponseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListRepositories_WhenRateLimited_ExposesRetryAfter()
    {
        var retryAfter = TimeSpan.FromSeconds(30);
        var handler = new StubHttpMessageHandler(
            "{\"message\":\"Too many requests\"}",
            HttpStatusCode.TooManyRequests,
            retryAfter);
        await using var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AzureDevOpsApiException>(
            () => client.ListRepositoriesAsync(
                project: null,
                new RepositoryQuery(),
                new PaginationOptions(),
                CancellationToken.None));

        Assert.Equal(retryAfter, exception.RetryAfter);
    }

    private static AzureDevOpsApiClient CreateClient(HttpMessageHandler handler)
    {
        var options = new AzureDevOpsOptions
        {
            OrganizationUrl = "https://dev.azure.com/test-organization",
            PersonalAccessToken = "test-token",
        };

        var restClient = new HttpClient(handler)
        {
            BaseAddress = options.CreateOrganizationUri(),
        };

        var gitClient = new GitHttpClient(
            options.CreateOrganizationUri(),
            new VssBasicCredential(string.Empty, options.PersonalAccessToken));

        return new AzureDevOpsApiClient(options, gitClient, restClient);
    }

    private sealed class StubHttpMessageHandler(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        TimeSpan? retryAfter = null) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };

            if (retryAfter is { } delay)
            {
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(delay);
            }

            return Task.FromResult(response);
        }
    }
}
