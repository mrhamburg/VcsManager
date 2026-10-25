using NSubstitute;

namespace VcsManager.Tests;

public class TimeoutLayerTests
{
    [Fact]
    public async Task ThrowsTimeoutException_WhenOperationDoesNotComplete()
    {
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), callInfo.Arg<CancellationToken>());
                return true;
            });

        var provider = new TimeoutLayer(new TimeoutLayerOptions
        {
            Timeout = TimeSpan.FromMilliseconds(50),
        }).Wrap(inner);

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" }));

        Assert.Contains("RepositoryExists", exception.Message);
    }

    [Fact]
    public async Task ThrowsTimeoutException_WhenOperationIgnoresCancellationToken()
    {
        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(completion.Task);

        var provider = new TimeoutLayer(new TimeoutLayerOptions
        {
            Timeout = TimeSpan.FromMilliseconds(50),
        }).Wrap(inner);

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => provider.RepositoryExistsAsync(new RepositoryRef { Name = "core" }));

        Assert.Contains("RepositoryExists", exception.Message);
    }

    [Fact]
    public async Task DoesNotConvertUserCancellationToTimeout()
    {
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, callInfo.Arg<CancellationToken>());
                return true;
            });

        var provider = new TimeoutLayer(new TimeoutLayerOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        }).Wrap(inner);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.RepositoryExistsAsync(
                new RepositoryRef { Name = "core" },
                cancellationSource.Token));
    }
}
