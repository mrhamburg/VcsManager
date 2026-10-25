using System.Diagnostics;
using System.Net;
using NSubstitute;

namespace VcsManager.Tests;

public class RetryLayerTests
{
    [Fact]
    public async Task RetriesTransientFailureAndSucceeds()
    {
        var calls = 0;
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return calls == 1
                    ? Task.FromException<bool>(Transient(HttpStatusCode.ServiceUnavailable))
                    : Task.FromResult(true);
            });

        var provider = new RetryLayer(FastOptions(maxAttempts: 2)).Wrap(inner);

        var result = await provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" });

        Assert.True(result);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task RetriesProviderMarkedTransientFailure()
    {
        var calls = 0;
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return calls == 1
                    ? Task.FromException<bool>(MappedTransient())
                    : Task.FromResult(true);
            });

        var provider = new RetryLayer(FastOptions(maxAttempts: 2)).Wrap(inner);

        var result = await provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" });

        Assert.True(result);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task DoesNotRetryUnmarkedProviderFailure()
    {
        var calls = 0;
        var exception = new VcsProviderException(
            "bad request",
            new InvalidOperationException("provider rejected the request"));
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return Task.FromException<bool>(exception);
            });

        var provider = new RetryLayer(FastOptions(maxAttempts: 3)).Wrap(inner);

        var thrown = await Assert.ThrowsAsync<VcsProviderException>(
            () => provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" }));

        Assert.Same(exception, thrown);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task DoesNotRetryNotFound()
    {
        var calls = 0;
        var exception = new VcsProviderException(
            "not found",
            new HttpRequestException("not found", null, HttpStatusCode.NotFound));
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return Task.FromException<bool>(exception);
            });

        var provider = new RetryLayer(FastOptions(maxAttempts: 3)).Wrap(inner);

        var thrown = await Assert.ThrowsAsync<VcsProviderException>(
            () => provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" }));

        Assert.Same(exception, thrown);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task DoesNotRetryMergeConflict()
    {
        var inner = Substitute.For<IVcsProvider>();
        inner.MergePullRequestAsync(
                Arg.Any<RepositoryRef>(),
                Arg.Any<int>(),
                Arg.Any<MergePullRequestRequest?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<PullRequest>(new VcsConflictException("conflict")));

        var provider = new RetryLayer(FastOptions(maxAttempts: 3)).Wrap(inner);

        await Assert.ThrowsAsync<VcsConflictException>(
            () => provider.MergePullRequestAsync(new RepositoryRef { Name = "core" }, 1));

        await inner.Received(1).MergePullRequestAsync(
            Arg.Any<RepositoryRef>(),
            1,
            Arg.Any<MergePullRequestRequest?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotRetryMergeEvenForRateLimit()
    {
        var inner = Substitute.For<IVcsProvider>();
        inner.MergePullRequestAsync(
                Arg.Any<RepositoryRef>(),
                Arg.Any<int>(),
                Arg.Any<MergePullRequestRequest?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<PullRequest>(
                new VcsRateLimitException("rate limit", TimeSpan.Zero)));

        var provider = new RetryLayer(FastOptions(maxAttempts: 3)).Wrap(inner);

        await Assert.ThrowsAsync<VcsRateLimitException>(
            () => provider.MergePullRequestAsync(new RepositoryRef { Name = "core" }, 1));

        await inner.Received(1).MergePullRequestAsync(
            Arg.Any<RepositoryRef>(),
            1,
            Arg.Any<MergePullRequestRequest?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotRetryRepositoryCreation()
    {
        var inner = Substitute.For<IVcsProvider>();
        inner.CreateRepositoryAsync(
                Arg.Any<CreateRepositoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<VcsRepository>(
                Transient(HttpStatusCode.ServiceUnavailable)));

        var provider = new RetryLayer(FastOptions(maxAttempts: 3)).Wrap(inner);

        await Assert.ThrowsAsync<VcsProviderException>(
            () => provider.CreateRepositoryAsync(new CreateRepositoryRequest { Name = "core" }));

        await inner.Received(1).CreateRepositoryAsync(
            Arg.Any<CreateRepositoryRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotRetryRepositoryDeletion()
    {
        var inner = Substitute.For<IVcsProvider>();
        inner.DeleteRepositoryAsync(
                Arg.Any<RepositoryRef>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(Transient(HttpStatusCode.ServiceUnavailable)));

        var provider = new RetryLayer(FastOptions(maxAttempts: 3)).Wrap(inner);

        await Assert.ThrowsAsync<VcsProviderException>(
            () => provider.DeleteRepositoryAsync(new RepositoryRef { Name = "core" }));

        await inner.Received(1).DeleteRepositoryAsync(
            Arg.Any<RepositoryRef>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotRetryPullRequestCreation()
    {
        var inner = Substitute.For<IVcsProvider>();
        inner.CreatePullRequestAsync(
                Arg.Any<RepositoryRef>(),
                Arg.Any<CreatePullRequestRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<PullRequest>(
                Transient(HttpStatusCode.ServiceUnavailable)));

        var provider = new RetryLayer(FastOptions(maxAttempts: 3)).Wrap(inner);

        await Assert.ThrowsAsync<VcsProviderException>(
            () => provider.CreatePullRequestAsync(
                new RepositoryRef { Name = "core" },
                new CreatePullRequestRequest
                {
                    Title = "title",
                    SourceBranch = "feature",
                    TargetBranch = "main",
                }));

        await inner.Received(1).CreatePullRequestAsync(
            Arg.Any<RepositoryRef>(),
            Arg.Any<CreatePullRequestRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HonorsRetryAfterDelay()
    {
        var calls = 0;
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return calls == 1
                    ? Task.FromException<bool>(
                        new VcsRateLimitException("rate limit", TimeSpan.FromMilliseconds(60)))
                    : Task.FromResult(true);
            });

        var provider = new RetryLayer(FastOptions(maxAttempts: 2)).Wrap(inner);
        var stopwatch = Stopwatch.StartNew();

        var result = await provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" });

        stopwatch.Stop();
        Assert.True(result);
        Assert.Equal(2, calls);
        Assert.True(
            stopwatch.Elapsed >= TimeSpan.FromMilliseconds(40),
            $"Expected a delay near the RetryAfter value, but elapsed {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task StopsAfterMaximumAttempts()
    {
        var calls = 0;
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return Task.FromException<bool>(Transient(HttpStatusCode.BadGateway));
            });

        var provider = new RetryLayer(FastOptions(maxAttempts: 3)).Wrap(inner);

        await Assert.ThrowsAsync<VcsProviderException>(
            () => provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" }));

        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task RetryPredicateCanVetoRetry()
    {
        var calls = 0;
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return Task.FromException<bool>(Transient(HttpStatusCode.InternalServerError));
            });

        var options = FastOptions(maxAttempts: 3);
        options.RetryPredicate = _ => false;
        var provider = new RetryLayer(options).Wrap(inner);

        await Assert.ThrowsAsync<VcsProviderException>(
            () => provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" }));

        Assert.Equal(1, calls);
    }

    private static RetryLayerOptions FastOptions(int maxAttempts)
    {
        return new RetryLayerOptions
        {
            MaxAttempts = maxAttempts,
            BaseDelay = TimeSpan.FromMilliseconds(1),
            MaxDelay = TimeSpan.FromMilliseconds(1),
            JitterFactor = 0,
        };
    }

    private static VcsProviderException Transient(HttpStatusCode statusCode)
    {
        return new VcsProviderException(
            "transient",
            new HttpRequestException("transient", null, statusCode));
    }

    private static VcsProviderException MappedTransient()
    {
        return new VcsProviderException(
            "service unavailable",
            new InvalidOperationException("provider returned HTTP 503"),
            isTransient: true);
    }
}
