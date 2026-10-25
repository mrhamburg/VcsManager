using System.Net;
using System.Text;
using System.Text.Json;
using VcsManager.GitLab.Internal;

namespace VcsManager.GitLab.Tests;

public sealed class GitLabApiClientTests
{
    [Fact]
    public async Task GetProject_EncodesNestedNamespaceAndSendsToken()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {
              "id": 42,
              "name": "Project",
              "path": "project",
              "path_with_namespace": "group/subgroup/project",
              "web_url": "https://gitlab.test/group/subgroup/project",
              "visibility": "private"
            }
            """));
        await using var client = CreateClient(handler);

        var project = await client.GetProjectAsync("group/subgroup/project", CancellationToken.None);

        Assert.Equal(42, project.Id);
        Assert.Equal("group", project.Owner);
        Assert.Equal("subgroup", project.Project);
        Assert.Equal("group/subgroup/project", project.PathWithNamespace);
        Assert.Equal("test-token", handler.LastRequest?.Headers.GetValues("PRIVATE-TOKEN").Single());
        Assert.Equal(
            "/api/v4/projects/group%2Fsubgroup%2Fproject",
            handler.LastRequest?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task ListProjects_ForwardsPaginationFiltersAndReadsHeaders()
    {
        var handler = new RecordingHandler(_ => JsonResponse(
            """
            [
              {
                "id": 1,
                "name": "Alpha",
                "path": "alpha",
                "path_with_namespace": "group/alpha",
                "web_url": "https://gitlab.test/group/alpha",
                "visibility": "public",
                "archived": false
              }
            ]
            """,
            ("X-Total", "6"),
            ("X-Next-Page", "2")));
        await using var client = CreateClient(handler);

        var page = await client.ListProjectsAsync(
            "group",
            new RepositoryQuery
            {
                Visibility = VcsVisibility.Public,
                NameContains = "alpha",
            },
            new PaginationOptions { Page = 1, PageSize = 5 },
            CancellationToken.None);

        Assert.Single(page.Items);
        Assert.Equal(6, page.TotalCount);
        Assert.True(page.HasNextPage);

        var uri = handler.LastRequest!.RequestUri!;
        var query = ParseQuery(uri);
        Assert.Equal("/api/v4/groups/group/projects", uri.AbsolutePath);
        Assert.Equal("1", query["page"]);
        Assert.Equal("5", query["per_page"]);
        Assert.Equal("true", query["include_subgroups"]);
        Assert.Equal("false", query["archived"]);
        Assert.Equal("public", query["visibility"]);
        Assert.Equal("alpha", query["search"]);
    }

    [Fact]
    public async Task CreateProject_WithNumericNamespace_SendsNamespaceId()
    {
        string? body = null;
        var handler = new RecordingHandler(request =>
        {
            body = request.Content is null
                ? null
                : new StreamReader(request.Content.ReadAsStream()).ReadToEnd();

            return JsonResponse("""
                {
                  "id": 9,
                  "name": "Project",
                  "path": "project",
                  "path_with_namespace": "123/project",
                  "web_url": "https://gitlab.test/123/project",
                  "visibility": "private"
                }
                """);
        });
        await using var client = CreateClient(handler);

        await client.CreateProjectAsync(
            "123",
            new CreateRepositoryRequest
            {
                Name = "project",
                Description = "Description",
                Visibility = VcsVisibility.Internal,
                InitializeWithReadme = true,
                DefaultBranch = "main",
            },
            CancellationToken.None);

        Assert.NotNull(body);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        Assert.Equal("project", root.GetProperty("name").GetString());
        Assert.Equal(123, root.GetProperty("namespace_id").GetInt64());
        Assert.Equal("internal", root.GetProperty("visibility").GetString());
        Assert.True(root.GetProperty("initialize_with_readme").GetBoolean());
        Assert.Equal("main", root.GetProperty("default_branch").GetString());
    }

    [Fact]
    public async Task MergeMergeRequest_SendsSquashAndBranchDeletion()
    {
        string? body = null;
        var handler = new RecordingHandler(request =>
        {
            body = new StreamReader(request.Content!.ReadAsStream()).ReadToEnd();
            return JsonResponse("""
                {
                  "id": 5,
                  "iid": 2,
                  "title": "Change",
                  "source_branch": "feature",
                  "target_branch": "main",
                  "state": "merged",
                  "merged_at": "2024-01-01T00:00:00Z",
                  "author": { "username": "user" },
                  "web_url": "https://gitlab.test/group/project/-/merge_requests/2"
                }
                """);
        });
        await using var client = CreateClient(handler);

        var mergeRequest = await client.MergeMergeRequestAsync(
            "group/project",
            2,
            new MergePullRequestRequest
            {
                Method = MergeMethod.Squash,
                DeleteSourceBranch = true,
                CommitTitle = "Title",
                CommitMessage = "Message",
            },
            CancellationToken.None);

        Assert.Equal(PullRequestState.Merged, mergeRequest.State);

        Assert.NotNull(body);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        Assert.True(root.GetProperty("squash").GetBoolean());
        Assert.True(root.GetProperty("should_remove_source_branch").GetBoolean());
        Assert.Equal("Title\n\nMessage", root.GetProperty("merge_commit_message").GetString());
    }

    [Fact]
    public async Task NonSuccessResponse_ThrowsGitLabApiExceptionWithRetryAfter()
    {
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("Rate limit exceeded", Encoding.UTF8, "text/plain"),
            };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                TimeSpan.FromSeconds(12));
            return response;
        });
        await using var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<GitLabApiException>(
            () => client.GetProjectAsync("group/project", CancellationToken.None));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(12), exception.RetryAfter);
    }

    private static GitLabApiClient CreateClient(HttpMessageHandler handler)
    {
        return new GitLabApiClient(
            new GitLabOptions
            {
                Token = "test-token",
                BaseUrl = "https://gitlab.test",
            },
            handler);
    }

    private static HttpResponseMessage JsonResponse(string json, params (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        foreach (var (name, value) in headers)
        {
            response.Headers.TryAddWithoutValidation(name, value);
        }

        return response;
    }

    private static Dictionary<string, string> ParseQuery(Uri uri)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var query = uri.Query.TrimStart('?');

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            var key = Uri.UnescapeDataString(part[..separator]);
            var value = Uri.UnescapeDataString(part[(separator + 1)..]);
            result[key] = value;
        }

        return result;
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _response;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response)
        {
            _response = response;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_response(request));
        }
    }
}
