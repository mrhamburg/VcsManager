using System.Net;
using System.Text;
using VcsManager.Gitea.Internal;

namespace VcsManager.Gitea.Tests;

public sealed class GiteaApiClientTests
{
    [Fact]
    public async Task GetRepository_SendsTokenAndParsesSnakeCaseResponse()
    {
        var handler = new StubHttpMessageHandler(
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
            """);
        await using var client = CreateClient(handler);

        var repository = await client.GetRepositoryAsync(
            "test-owner",
            "test-repository",
            CancellationToken.None);

        Assert.Equal("test-owner/test-repository", repository.FullName);
        Assert.Equal("test-owner", repository.Owner);
        Assert.Equal(VcsVisibility.Private, repository.Visibility);
        Assert.Equal(new Uri("https://gitea.example.com/test-owner/test-repository"), new Uri(repository.WebUrl));
        Assert.Equal("/api/v1/repos/test-owner/test-repository", handler.RequestUri?.AbsolutePath);
        Assert.Equal("token test-token", handler.Authorization);
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

    internal sealed class StubHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            });
        }
    }
}
