using System.Diagnostics;
using System.Diagnostics.Metrics;
using NSubstitute;

namespace VcsManager.Tests;

public class OtelLayerTests
{
    [Fact]
    public async Task Metrics_UseConfiguredNamesAndRecordOutcomes()
    {
        var meterName = $"VcsManager.Tests.{Guid.NewGuid():N}";
        var instrumentNames = new List<string>();
        long calls = 0;
        long failures = 0;
        double duration = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name != meterName)
            {
                return;
            }

            instrumentNames.Add(instrument.Name);
            meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name.EndsWith("operation.calls", StringComparison.Ordinal))
            {
                calls += value;
            }
            else if (instrument.Name.EndsWith("operation.failures", StringComparison.Ordinal))
            {
                failures += value;
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (instrument.Name.EndsWith("operation.duration_ms", StringComparison.Ordinal))
            {
                duration = value;
            }
        });
        listener.Start();

        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(true);
        inner.GetRepositoryAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<VcsRepository>(
                new VcsProviderException("failure")));
        var provider = new OtelMetricsLayer(new OtelMetricsLayerOptions
        {
            MeterName = meterName,
            MetricPrefix = "test.",
            ProviderName = "github",
        }).Wrap(inner);

        await provider.RepositoryExistsAsync(new RepositoryRef { Owner = "acme", Name = "core" });
        await Assert.ThrowsAsync<VcsProviderException>(
            () => provider.GetRepositoryAsync(new RepositoryRef { Owner = "acme", Name = "core" }));

        Assert.Contains("test.operation.calls", instrumentNames);
        Assert.Contains("test.operation.failures", instrumentNames);
        Assert.Contains("test.operation.duration_ms", instrumentNames);
        Assert.Equal(2, calls);
        Assert.Equal(1, failures);
        Assert.True(duration >= 0);
    }

    [Fact]
    public async Task Trace_UsesConfiguredSourceAndMarksFailures()
    {
        var sourceName = $"VcsManager.Tests.{Guid.NewGuid():N}";
        Activity? captured = null;

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => captured = activity,
        };
        ActivitySource.AddActivityListener(listener);

        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(true);
        inner.GetRepositoryAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<VcsRepository>(
                new VcsProviderException("failure")));
        var provider = new OtelTraceLayer(new OtelTraceLayerOptions
        {
            ActivitySourceName = sourceName,
            ProviderName = "github",
        }).Wrap(inner);

        await provider.RepositoryExistsAsync(new RepositoryRef { Owner = "acme", Name = "core" });

        Assert.NotNull(captured);
        Assert.Equal("RepositoryExists", captured.DisplayName);
        Assert.Equal("github", captured.GetTagItem("vcs.provider"));
        Assert.Equal("acme/core", captured.GetTagItem("vcs.repository"));
        Assert.Equal(ActivityStatusCode.Ok, captured.Status);

        captured = null;
        await Assert.ThrowsAsync<VcsProviderException>(
            () => provider.GetRepositoryAsync(new RepositoryRef { Owner = "acme", Name = "core" }));

        Assert.NotNull(captured);
        Assert.Equal("GetRepository", captured.DisplayName);
        Assert.Equal(ActivityStatusCode.Error, captured.Status);
        Assert.Equal(typeof(VcsProviderException).FullName, captured.GetTagItem("exception.type"));
    }
}
