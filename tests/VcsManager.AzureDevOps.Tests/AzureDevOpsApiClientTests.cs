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

        Assert.Empty(page.Items);
        Assert.False(page.HasNextPage);
        Assert.Null(page.TotalCount);

        var uri = Assert.IsType<Uri>(handler.RequestUri);
        Assert.Equal(
            $"/{TestData.Organization}/_apis/git/repositories",
            uri.AbsolutePath);
        Assert.Contains("api-version=7.1", uri.Query, StringComparison.Ordinal);
        Assert.DoesNotContain("top", uri.Query, StringComparison.Ordinal);
        Assert.DoesNotContain("skip", uri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListRepositories_WithoutProject_MapsProjectAndOrganization()
    {
        var handler = new StubHttpMessageHandler(RepositoryJson);
        await using var client = CreateClient(handler);

        var page = await client.ListRepositoriesAsync(
            project: null,
            new RepositoryQuery(),
            new PaginationOptions(),
            CancellationToken.None);

        var repository = Assert.Single(page.Items);
        Assert.Equal("test-project", repository.Project);
        Assert.Equal("test-organization", repository.Organization);
    }

    [Fact]
    public async Task ListRepositories_PagesOnTheClient()
    {
        var handler = new StubHttpMessageHandler(RepositoriesJson(5));
        await using var client = CreateClient(handler);

        var first = await client.ListRepositoriesAsync(
            project: null,
            new RepositoryQuery(),
            new PaginationOptions { Page = 1, PageSize = 2 },
            CancellationToken.None);
        var second = await client.ListRepositoriesAsync(
            project: null,
            new RepositoryQuery(),
            new PaginationOptions { Page = 2, PageSize = 2 },
            CancellationToken.None);
        var third = await client.ListRepositoriesAsync(
            project: null,
            new RepositoryQuery(),
            new PaginationOptions { Page = 3, PageSize = 2 },
            CancellationToken.None);

        Assert.Equal(["repository-1", "repository-2"], first.Items.Select(item => item.Name));
        Assert.Equal(["repository-3", "repository-4"], second.Items.Select(item => item.Name));
        Assert.Equal(["repository-5"], third.Items.Select(item => item.Name));
        Assert.True(first.HasNextPage);
        Assert.True(second.HasNextPage);
        Assert.False(third.HasNextPage);

        var query = Assert.IsType<Uri>(handler.RequestUri).Query;
        Assert.DoesNotContain("skip", query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("top", query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListRepositories_WithVeryLargePage_ReturnsEmptyPage()
    {
        var handler = new StubHttpMessageHandler(RepositoriesJson(3));
        await using var client = CreateClient(handler);

        var page = await client.ListRepositoriesAsync(
            project: null,
            new RepositoryQuery(),
            new PaginationOptions { Page = int.MaxValue, PageSize = 100 },
            CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public async Task ListRepositories_WithoutWebUrl_EscapesProjectAndName()
    {
        var handler = new StubHttpMessageHandler(
            """
            {
              "count": 1,
              "value": [
                {
                  "id": "11111111-1111-1111-1111-111111111111",
                  "name": "my repo",
                  "project": { "id": "33333333-3333-3333-3333-333333333333", "name": "My Project" }
                }
              ]
            }
            """);
        await using var client = CreateClient(handler);

        var page = await client.ListRepositoriesAsync(
            project: null,
            new RepositoryQuery(),
            new PaginationOptions(),
            CancellationToken.None);

        Assert.Equal(
            "https://dev.azure.com/test-organization/My%20Project/_git/my%20repo",
            Assert.Single(page.Items).WebUrl);
    }

    [Theory]
    [InlineData(HttpStatusCode.NonAuthoritativeInformation)]
    [InlineData(HttpStatusCode.OK)]
    public async Task ListRepositories_WhenSignInPageIsReturned_ThrowsUnauthorized(
        HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler(
            "<html><body>Sign in</body></html>",
            statusCode,
            contentType: "text/html");
        await using var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AzureDevOpsApiException>(
            () => client.ListRepositoriesAsync(
                project: null,
                new RepositoryQuery(),
                new PaginationOptions(),
                CancellationToken.None));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Empty(exception.ResponseBody);
    }

    [Fact]
    public async Task ListRepositories_WhenSignInPageIsReturned_ProviderThrowsAuthenticationException()
    {
        var handler = new StubHttpMessageHandler(
            "<html><body>Sign in</body></html>",
            HttpStatusCode.NonAuthoritativeInformation,
            contentType: "text/html");
        var client = CreateClient(handler);
        var options = new AzureDevOpsOptions
        {
            OrganizationUrl = "https://dev.azure.com/test-organization",
            PersonalAccessToken = "test-token",
        };
        await using var provider = new AzureDevOpsProvider(client, options);

        var exception = await Assert.ThrowsAsync<VcsAuthenticationException>(
            () => provider.ListRepositoriesAsync());

        Assert.DoesNotContain("Sign in", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListRepositories_WhenHtmlErrorPageIsReturned_KeepsOriginalStatus()
    {
        var handler = new StubHttpMessageHandler(
            "<html>Bad gateway</html>",
            HttpStatusCode.BadGateway,
            contentType: "text/html");
        await using var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AzureDevOpsApiException>(
            () => client.ListRepositoriesAsync(
                project: null,
                new RepositoryQuery(),
                new PaginationOptions(),
                CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
    }

    [Fact]
    public async Task SeedRepository_WhenSignInPageIsReturned_ThrowsUnauthorized()
    {
        var handler = new SeedHandler(_ => new HttpResponseMessage(
            HttpStatusCode.NonAuthoritativeInformation)
        {
            Content = new StringContent("<html></html>", Encoding.UTF8, "text/html"),
        });
        await using var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AzureDevOpsApiException>(
            () => client.SeedRepositoryAsync(
                TestData.Project,
                "test-repository",
                new SeedRepositoryRequest { Branch = "seeded", Files = [] },
                CancellationToken.None));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
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

    private static string RepositoriesJson(int count)
    {
        var items = string.Join(
            ",",
            Enumerable.Range(1, count).Select(index =>
                $$"""
                {
                  "id": "11111111-1111-1111-1111-11111111111{{index}}",
                  "name": "repository-{{index}}",
                  "webUrl": "https://dev.azure.com/test-organization/test-project/_git/repository-{{index}}",
                  "project": { "id": "33333333-3333-3333-3333-333333333333", "name": "test-project" }
                }
                """));

        return $$"""{"count": {{count}}, "value": [{{items}}]}""";
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
        TimeSpan? retryAfter = null,
        string contentType = "application/json") : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, contentType),
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
