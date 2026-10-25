using System.Net;
using System.Text;
using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaApiClientTests
{
    private const string RepositoryJson =
        """
        {
          "id": 42,
          "name": "test-repository",
          "full_name": "test-owner/test-repository",
          "html_url": "https://gitea.example.com/test-owner/test-repository",
          "default_branch": "main",
          "private": true,
          "archived": false,
          "owner": { "id": 1, "login": "test-owner" }
        }
        """;

    [Fact]
    public async Task GetRepository_SendsTokenAndParsesSnakeCaseResponse()
    {
        var handler = new StubGiteaHandler();
        handler.Map("/api/v1/repos/test-owner/test-repository", RepositoryJson);
        await using var client = CreateClient(handler);

        var repository = await client.GetRepositoryAsync(
            "test-owner",
            "test-repository",
            CancellationToken.None);

        Assert.Equal("test-owner/test-repository", repository.FullName);
        Assert.Equal("test-owner", repository.Owner);
        Assert.Equal(VcsVisibility.Private, repository.Visibility);
        Assert.Equal(
            new Uri("https://gitea.example.com/test-owner/test-repository"),
            new Uri(repository.WebUrl));
        Assert.Equal("token test-token", handler.Authorization);
        Assert.Equal(
            "/api/v1/repos/test-owner/test-repository",
            handler.Requests[0].AbsolutePath);
    }

    [Fact]
    public async Task ListRepositories_ForOrganization_UsesOrganizationEndpoint()
    {
        var handler = new StubGiteaHandler();
        handler.Map("/api/v1/orgs/test-owner", """{"id": 10, "username": "test-owner"}""");
        handler.Map(
            "/api/v1/orgs/test-owner/repos",
            $"[{RepositoryJson}]",
            headers:
            [
                ("Link", "<https://gitea.example.com/api/v1/orgs/test-owner/repos?page=2&limit=50>; rel=\"next\""),
                ("X-Total-Count", "60"),
            ]);
        await using var client = CreateClient(handler);

        var page = await client.ListRepositoriesAsync(
            "test-owner",
            new RepositoryQuery(),
            new PaginationOptions { PageSize = 100 },
            CancellationToken.None);

        Assert.Single(page.Items);
        Assert.True(page.HasNextPage);
        Assert.Equal(60, page.TotalCount);
        Assert.Equal("/api/v1/orgs/test-owner", handler.Requests[0].AbsolutePath);
        Assert.Equal("/api/v1/orgs/test-owner/repos", handler.Requests[1].AbsolutePath);
        Assert.Contains("limit=50", handler.Requests[1].Query, StringComparison.Ordinal);
        Assert.Contains("page=1", handler.Requests[1].Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListRepositories_ForUser_ResolvesUserIdAndSearches()
    {
        var handler = new StubGiteaHandler();
        handler.Map("/api/v1/orgs/test-owner", "{}", HttpStatusCode.NotFound);
        handler.Map("/api/v1/users/test-owner", """{"id": 5, "login": "test-owner"}""");
        handler.Map("/api/v1/repos/search", $$"""{"ok": true, "data": [{{RepositoryJson}}]}""");
        await using var client = CreateClient(handler);

        var page = await client.ListRepositoriesAsync(
            "test-owner",
            new RepositoryQuery
            {
                NameContains = "repo",
                Visibility = VcsVisibility.Private,
                IncludeArchived = false,
            },
            new PaginationOptions { Page = 3, PageSize = 10 },
            CancellationToken.None);

        Assert.Single(page.Items);
        Assert.Equal("/api/v1/users/test-owner", handler.Requests[1].AbsolutePath);

        var searchRequest = handler.Requests[2];
        Assert.Equal("/api/v1/repos/search", searchRequest.AbsolutePath);
        Assert.Contains("uid=5", searchRequest.Query, StringComparison.Ordinal);
        Assert.Contains("q=repo", searchRequest.Query, StringComparison.Ordinal);
        Assert.Contains("is_private=true", searchRequest.Query, StringComparison.Ordinal);
        Assert.Contains("archived=false", searchRequest.Query, StringComparison.Ordinal);
        Assert.Contains("page=3", searchRequest.Query, StringComparison.Ordinal);
        Assert.Contains("limit=10", searchRequest.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListRepositories_WithoutOwner_SearchesVisibleRepositories()
    {
        var handler = new StubGiteaHandler();
        handler.Map("/api/v1/repos/search", $$"""{"ok": true, "data": [{{RepositoryJson}}]}""");
        await using var client = CreateClient(handler);

        var page = await client.ListRepositoriesAsync(
            owner: null,
            new RepositoryQuery { Visibility = VcsVisibility.Public, IncludeArchived = true },
            new PaginationOptions { PageSize = 5 },
            CancellationToken.None);

        Assert.Single(page.Items);
        Assert.Single(handler.Requests);
        Assert.Equal("/api/v1/repos/search", handler.Requests[0].AbsolutePath);
        Assert.Contains("is_private=false", handler.Requests[0].Query, StringComparison.Ordinal);
        Assert.DoesNotContain("archived=", handler.Requests[0].Query, StringComparison.Ordinal);
        Assert.Contains("limit=5", handler.Requests[0].Query, StringComparison.Ordinal);
    }

    internal static GiteaApiClient CreateClient(HttpMessageHandler handler)
    {
        return new GiteaApiClient(
            new GiteaOptions
            {
                BaseUrl = "https://gitea.example.com",
                Token = "test-token",
            },
            handler);
    }

    internal sealed class StubGiteaHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, StubResponse> _responses = new(StringComparer.Ordinal);

        public List<Uri> Requests { get; } = [];

        public string? Authorization { get; private set; }

        public void Map(
            string path,
            string responseBody,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            (string Name, string Value)[]? headers = null)
        {
            _responses[path] = new StubResponse(responseBody, statusCode, headers ?? []);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request.RequestUri);
            Requests.Add(request.RequestUri);
            Authorization = request.Headers.Authorization?.ToString();

            if (!_responses.TryGetValue(request.RequestUri.AbsolutePath, out var response))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                });
            }

            var message = new HttpResponseMessage(response.StatusCode)
            {
                Content = new StringContent(response.Body, Encoding.UTF8, "application/json"),
            };

            foreach (var header in response.Headers)
            {
                message.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }

            return Task.FromResult(message);
        }

        private sealed record StubResponse(
            string Body,
            HttpStatusCode StatusCode,
            IReadOnlyList<(string Name, string Value)> Headers);
    }
}
