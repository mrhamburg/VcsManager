using System.Net;
using System.Text;
using System.Text.Json;
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


    [Fact]
    public async Task SeedRepository_PushesInitialCommit()
    {
        string? pushBody = null;
        var handler = new SeedHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;

            if (request.Method == HttpMethod.Get &&
                path.EndsWith("/refs", StringComparison.Ordinal))
            {
                return SeedHandler.Json("""{ "count": 0, "value": [] }""");
            }

            if (request.Method == HttpMethod.Post &&
                path.EndsWith("/pushes", StringComparison.Ordinal))
            {
                pushBody = new StreamReader(request.Content!.ReadAsStream()).ReadToEnd();
                return SeedHandler.Json("""{ "commits": [] }""", HttpStatusCode.Created);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        await using var client = CreateClient(handler);

        await client.SeedRepositoryAsync(
            TestData.Project,
            "test-repository",
            new SeedRepositoryRequest
            {
                Branch = "seeded",
                Files =
                [
                    new RepositorySeedFile
                    {
                        Path = "README.md",
                        Content = "hello"u8.ToArray(),
                    },
                ],
            },
            CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(
            $"/{TestData.Organization}/{TestData.Project}/_apis/git/repositories/test-repository/refs",
            handler.Requests[0].AbsolutePath);
        Assert.Contains("filter=heads%2F", handler.Requests[0].Query, StringComparison.Ordinal);
        Assert.Equal(
            $"/{TestData.Organization}/{TestData.Project}/_apis/git/repositories/test-repository/pushes",
            handler.Requests[1].AbsolutePath);

        Assert.NotNull(pushBody);
        using var json = JsonDocument.Parse(pushBody);
        var root = json.RootElement;
        var refUpdate = root.GetProperty("refUpdates")[0];
        Assert.Equal("refs/heads/seeded", refUpdate.GetProperty("name").GetString());
        Assert.Equal(new string('0', 40), refUpdate.GetProperty("oldObjectId").GetString());

        var commit = root.GetProperty("commits")[0];
        Assert.Equal("Initial commit", commit.GetProperty("comment").GetString());
        var change = commit.GetProperty("changes")[0];
        Assert.Equal("add", change.GetProperty("changeType").GetString());
        Assert.Equal("/README.md", change.GetProperty("item").GetProperty("path").GetString());
        var content = change.GetProperty("newContent");
        Assert.Equal("base64Encoded", content.GetProperty("contentType").GetString());
        Assert.Equal(
            "hello",
            Encoding.UTF8.GetString(Convert.FromBase64String(content.GetProperty("content").GetString()!)));
    }

    [Fact]
    public async Task SeedRepository_WhenRepositoryIsNotEmpty_ThrowsConflict()
    {
        var handler = new SeedHandler(_ => SeedHandler.Json("""
            {
              "count": 1,
              "value": [ { "name": "refs/heads/main" } ]
            }
            """));
        await using var client = CreateClient(handler);

        await Assert.ThrowsAsync<VcsConflictException>(
            () => client.SeedRepositoryAsync(
                TestData.Project,
                "test-repository",
                new SeedRepositoryRequest
                {
                    Branch = "seeded",
                    Files =
                    [
                        new RepositorySeedFile
                        {
                            Path = "README.md",
                            Content = "hello"u8.ToArray(),
                        },
                    ],
                },
                CancellationToken.None));

        Assert.Single(handler.Requests);
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

    private sealed class SeedHandler(Func<HttpRequestMessage, HttpResponseMessage> response)
        : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(response(request));
        }

        public static HttpResponseMessage Json(
            string body,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }
}
