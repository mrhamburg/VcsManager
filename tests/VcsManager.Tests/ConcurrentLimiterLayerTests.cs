using NSubstitute;

namespace VcsManager.Tests;

public class ConcurrentLimiterLayerTests
{
    [Fact]
    public async Task RespectsMaximumConcurrency()
    {
        var current = 0;
        var maximum = 0;
        var gate = new object();
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                var now = Interlocked.Increment(ref current);
                lock (gate)
                {
                    maximum = Math.Max(maximum, now);
                }

                await Task.Delay(50);
                Interlocked.Decrement(ref current);
                return true;
            });

        var provider = new ConcurrentLimiterLayer(new ConcurrentLimiterLayerOptions
        {
            MaxConcurrentRequests = 2,
        }).Wrap(inner);

        await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(_ => provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" })));

        Assert.Equal(2, maximum);
    }


    [Fact]
    public async Task LimitsConcurrentSeedOperations()
    {
        var current = 0;
        var maximum = 0;
        var inner = Substitute.For<IVcsProvider>();
        inner.SeedRepositoryAsync(
                Arg.Any<RepositoryRef>(),
                Arg.Any<SeedRepositoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                var now = Interlocked.Increment(ref current);
                InterlockedMax(ref maximum, now);
                await Task.Delay(50);
                Interlocked.Decrement(ref current);
            });

        var provider = new ConcurrentLimiterLayer(new ConcurrentLimiterLayerOptions
        {
            MaxConcurrentRequests = 2,
        }).Wrap(inner);
        var request = new SeedRepositoryRequest
        {
            Branch = "main",
            Files =
            [
                new RepositorySeedFile
                {
                    Path = "README.md",
                    Content = "seed"u8.ToArray(),
                },
            ],
        };

        await Task.WhenAll(
            Enumerable.Range(0, 6)
                .Select(_ => provider.SeedRepositoryAsync(
                    new RepositoryRef { Name = "core" },
                    request)));

        Assert.Equal(2, maximum);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int observed;
        do
        {
            observed = target;
            if (value <= observed)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref target, value, observed) != observed);
    }

    [Fact]
    public async Task ThrowsTransientProviderException_WhenSlotIsNotAvailable()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await release.Task;
                return true;
            });

        var provider = new ConcurrentLimiterLayer(new ConcurrentLimiterLayerOptions
        {
            MaxConcurrentRequests = 1,
            AcquireTimeout = TimeSpan.FromMilliseconds(30),
        }).Wrap(inner);

        var first = provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" });
        await Task.Delay(50);

        try
        {
            var exception = await Assert.ThrowsAsync<VcsProviderException>(
                () => provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" }));

            Assert.True(exception.IsTransient);
            Assert.IsType<TimeoutException>(exception.InnerException);
        }
        finally
        {
            release.SetResult();
            await first;
        }
    }
}
