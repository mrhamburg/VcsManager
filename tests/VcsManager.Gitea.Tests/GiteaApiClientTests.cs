using System.Net;
using System.Text;
using System.Text.Json;
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
    public async Task CreatePullRequest_AsDraft_PrefixesWipTitle()
    {
        var handler = new StubGiteaHandler();
        handler.Map(
            "/api/v1/repos/test-owner/test-repository/pulls",
            """
            {
              "id": 107,
              "number": 7,
              "title": "WIP: Draft change",
              "state": "open",
              "draft": true,
              "user": { "id": 2, "login": "test-user" },
              "head": { "ref": "feature" },
              "base": { "ref": "main" }
            }
            """);
        await using var client = CreateClient(handler);

        var pullRequest = await client.CreatePullRequestAsync(
            "test-owner",
            "test-repository",
            new CreatePullRequestRequest
            {
                Title = "Draft change",
                Description = "A draft pull request",
                SourceBranch = "feature",
                TargetBranch = "main",
                Draft = true,
            },
            CancellationToken.None);

        Assert.Equal(PullRequestState.Draft, pullRequest.State);
        Assert.True(pullRequest.IsDraft);
        Assert.NotNull(handler.LastBody);
        Assert.Contains("\"head\":\"feature\"", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("\"base\":\"main\"", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("\"title\":\"WIP: Draft change\"", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("\"body\":\"A draft pull request\"", handler.LastBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListPullRequests_MapsFiltersAndResponse()
    {
        var handler = new StubGiteaHandler();
        handler.Map(
            "/api/v1/repos/test-owner/test-repository/pulls",
            """
            [
              {
                "id": 108,
                "number": 8,
                "title": "Merged change",
                "state": "closed",
                "merged": true,
                "user": { "id": 2, "login": "test-user" },
                "head": { "ref": "feature" },
                "base": { "ref": "main" }
              }
            ]
            """);
        await using var client = CreateClient(handler);

        var page = await client.ListPullRequestsAsync(
            "test-owner",
            "test-repository",
            new PullRequestQuery
            {
                State = PullRequestState.Merged,
                Author = "test-user",
                TargetBranch = "main",
            },
            new PaginationOptions { Page = 2, PageSize = 100 },
            CancellationToken.None);

        var request = handler.Requests[0];
        Assert.Equal("/api/v1/repos/test-owner/test-repository/pulls", request.AbsolutePath);
        Assert.Contains("state=closed", request.Query, StringComparison.Ordinal);
        Assert.Contains("poster=test-user", request.Query, StringComparison.Ordinal);
        Assert.Contains("base_branch=main", request.Query, StringComparison.Ordinal);
        Assert.Contains("page=2", request.Query, StringComparison.Ordinal);
        Assert.Contains("limit=50", request.Query, StringComparison.Ordinal);
        Assert.Equal(PullRequestState.Merged, page.Items[0].State);
    }

    [Fact]
    public async Task MergePullRequest_SendsMergeStyleAndReturnsRefreshedPullRequest()
    {
        var handler = new StubGiteaHandler();
        handler.Map("/api/v1/repos/test-owner/test-repository/pulls/7/merge", "{}");
        handler.Map(
            "/api/v1/repos/test-owner/test-repository/pulls/7",
            """
            {
              "id": 107,
              "number": 7,
              "title": "Merged change",
              "state": "closed",
              "merged": true,
              "user": { "id": 2, "login": "test-user" },
              "head": { "ref": "feature" },
              "base": { "ref": "main" }
            }
            """);
        await using var client = CreateClient(handler);

        var merged = await client.MergePullRequestAsync(
            "test-owner",
            "test-repository",
            7,
            new MergePullRequestRequest
            {
                Method = MergeMethod.Squash,
                DeleteSourceBranch = true,
                CommitTitle = "Complete change",
                CommitMessage = "Completed by VcsManager",
            },
            CancellationToken.None);

        Assert.Equal(PullRequestState.Merged, merged.State);
        Assert.Equal("/api/v1/repos/test-owner/test-repository/pulls/7/merge", handler.Requests[0].AbsolutePath);
        Assert.Equal("/api/v1/repos/test-owner/test-repository/pulls/7", handler.Requests[1].AbsolutePath);
        Assert.NotNull(handler.LastBody);
        Assert.Contains("\"do\":\"squash\"", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("\"delete_branch_after_merge\":true", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("\"merge_title_field\":\"Complete change\"", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("\"merge_message_field\":\"Completed by VcsManager\"", handler.LastBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClosePullRequest_SendsClosedState()
    {
        var handler = new StubGiteaHandler();
        handler.Map(
            "/api/v1/repos/test-owner/test-repository/pulls/8",
            """
            {
              "id": 108,
              "number": 8,
              "title": "Closed change",
              "state": "closed",
              "merged": false,
              "user": { "id": 2, "login": "test-user" },
              "head": { "ref": "feature" },
              "base": { "ref": "main" }
            }
            """);
        await using var client = CreateClient(handler);

        var closed = await client.ClosePullRequestAsync(
            "test-owner",
            "test-repository",
            8,
            CancellationToken.None);

        Assert.Equal(PullRequestState.Closed, closed.State);
        Assert.NotNull(handler.LastBody);
        Assert.Contains("\"state\":\"closed\"", handler.LastBody, StringComparison.Ordinal);
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


    [Fact]
    public async Task SeedRepository_CreatesOneCommitWithAllFiles()
    {
        var handler = new StubGiteaHandler();
        handler.Map(
            "/api/v1/repos/test-owner/test-repository",
            """
            {
              "id": 42,
              "name": "test-repository",
              "full_name": "test-owner/test-repository",
              "html_url": "https://gitea.example.com/test-owner/test-repository",
              "private": true,
              "empty": true,
              "owner": { "id": 1, "login": "test-owner" }
            }
            """);
        handler.Map(
            "/api/v1/repos/test-owner/test-repository/contents",
            """{ "commit": { "sha": "abc" } }""");
        await using var client = CreateClient(handler);

        await client.SeedRepositoryAsync(
            "test-owner",
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
        Assert.NotNull(handler.LastBody);
        using var json = JsonDocument.Parse(handler.LastBody);
        var root = json.RootElement;
        Assert.Equal("seeded", root.GetProperty("branch").GetString());
        Assert.Equal("Initial commit", root.GetProperty("message").GetString());
        var file = root.GetProperty("files")[0];
        Assert.Equal("create", file.GetProperty("operation").GetString());
        Assert.Equal("README.md", file.GetProperty("path").GetString());
        Assert.Equal(
            "hello",
            Encoding.UTF8.GetString(Convert.FromBase64String(file.GetProperty("content").GetString()!)));
    }

    [Fact]
    public async Task SeedRepository_WhenRepositoryIsNotEmpty_ThrowsConflict()
    {
        var handler = new StubGiteaHandler();
        handler.Map(
            "/api/v1/repos/test-owner/test-repository",
            """
            {
              "id": 42,
              "name": "test-repository",
              "full_name": "test-owner/test-repository",
              "html_url": "https://gitea.example.com/test-owner/test-repository",
              "private": true,
              "empty": false,
              "owner": { "id": 1, "login": "test-owner" }
            }
            """);
        await using var client = CreateClient(handler);

        await Assert.ThrowsAsync<VcsConflictException>(
            () => client.SeedRepositoryAsync(
                "test-owner",
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

    [Fact]
    public async Task CreateRepository_ForAuthenticatedUser_PostsToUserRepositories()
    {
        var handler = new StubGiteaHandler();
        handler.Map("/api/v1/orgs/test-owner", "{}", HttpStatusCode.NotFound);
        handler.Map("/api/v1/users/test-owner", """{"id": 5, "login": "test-owner"}""");
        handler.Map("/api/v1/user", """{"id": 5, "login": "test-owner"}""");
        handler.Map("/api/v1/user/repos", RepositoryJson);
        await using var client = CreateClient(handler);

        var repository = await client.CreateRepositoryAsync(
            "test-owner",
            new CreateRepositoryRequest
            {
                Name = "test-repository",
                Visibility = VcsVisibility.Private,
            },
            CancellationToken.None);

        Assert.Equal("test-owner/test-repository", repository.FullName);
        Assert.Equal("/api/v1/user/repos", handler.Requests[^1].AbsolutePath);
        Assert.NotNull(handler.LastBody);
        Assert.Contains("\"name\":\"test-repository\"", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("\"private\":true", handler.LastBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateRepository_ForAnotherUser_ThrowsUnsupportedOperation()
    {
        var handler = new StubGiteaHandler();
        handler.Map("/api/v1/orgs/other-user", "{}", HttpStatusCode.NotFound);
        handler.Map("/api/v1/users/other-user", """{"id": 6, "login": "other-user"}""");
        handler.Map("/api/v1/user", """{"id": 5, "login": "test-owner"}""");
        await using var client = CreateClient(handler);

        await Assert.ThrowsAsync<VcsUnsupportedOperationException>(
            () => client.CreateRepositoryAsync(
                "other-user",
                new CreateRepositoryRequest { Name = "test-repository" },
                CancellationToken.None));

        Assert.DoesNotContain(
            handler.Requests,
            request => request.AbsolutePath == "/api/v1/user/repos");
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

        public string? LastBody { get; private set; }

        public void Map(
            string path,
            string responseBody,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            (string Name, string Value)[]? headers = null)
        {
            _responses[path] = new StubResponse(responseBody, statusCode, headers ?? []);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request.RequestUri);
            Requests.Add(request.RequestUri);
            Authorization = request.Headers.Authorization?.ToString();

            if (request.Content is not null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            if (!_responses.TryGetValue(request.RequestUri.AbsolutePath, out var response))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                };
            }

            var message = new HttpResponseMessage(response.StatusCode)
            {
                Content = new StringContent(response.Body, Encoding.UTF8, "application/json"),
            };

            foreach (var header in response.Headers)
            {
                message.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }

            return message;
        }

        private sealed record StubResponse(
            string Body,
            HttpStatusCode StatusCode,
            IReadOnlyList<(string Name, string Value)> Headers);
    }
}
