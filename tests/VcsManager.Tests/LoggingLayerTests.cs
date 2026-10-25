using NSubstitute;

namespace VcsManager.Tests;

public class LoggingLayerTests
{
    [Fact]
    public async Task LogsStartAndSuccessWithSafeFields()
    {
        var logger = new RecordingLogger();
        var pullRequest = new PullRequest
        {
            Id = "7",
            Number = 7,
            Title = "Add feature",
            SourceBranch = "feature",
            TargetBranch = "main",
            Author = "author",
            Url = "https://example.test/core/pull/7",
        };
        var inner = Substitute.For<IVcsProvider>();
        inner.GetPullRequestAsync(Arg.Any<RepositoryRef>(), 7, Arg.Any<CancellationToken>())
            .Returns(pullRequest);
        var provider = new LoggingLayer(new LoggingLayerOptions
        {
            ProviderName = "github",
            Logger = logger,
        }).Wrap(inner);

        await provider.GetPullRequestAsync(
            new RepositoryRef { Owner = "acme", Project = "platform", Name = "core" },
            7);

        Assert.Contains("start:GetPullRequest:acme/platform/core:7:github", logger.Events);
        Assert.Contains("success:GetPullRequest:acme/platform/core:7:github", logger.Events);
    }


    [Fact]
    public async Task LogsSeedWithoutFileContent()
    {
        var logger = new RecordingLogger();
        var inner = Substitute.For<IVcsProvider>();
        inner.SeedRepositoryAsync(
                Arg.Any<RepositoryRef>(),
                Arg.Any<SeedRepositoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var provider = new LoggingLayer(new LoggingLayerOptions
        {
            ProviderName = "github",
            Logger = logger,
        }).Wrap(inner);

        await provider.SeedRepositoryAsync(
            new RepositoryRef { Owner = "acme", Name = "core" },
            new SeedRepositoryRequest
            {
                Branch = "main",
                Files =
                [
                    new RepositorySeedFile
                    {
                        Path = "README.md",
                        Content = "super-secret"u8.ToArray(),
                    },
                ],
            });

        Assert.Contains("start:SeedRepository:acme/core::github", logger.Events);
        Assert.Contains("success:SeedRepository:acme/core::github", logger.Events);
        Assert.DoesNotContain("super-secret", string.Join("|", logger.Events));
    }

    [Fact]
    public async Task DoesNotLogRequestContent()
    {
        var logger = new RecordingLogger();
        var inner = Substitute.For<IVcsProvider>();
        inner.CreateRepositoryAsync(Arg.Any<CreateRepositoryRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new VcsRepository
            {
                Id = "1",
                Name = callInfo.Arg<CreateRepositoryRequest>().Name,
                Owner = callInfo.Arg<CreateRepositoryRequest>().Owner,
                FullName = "acme/core",
                Url = "https://example.test/acme/core",
            }));
        var provider = new LoggingLayer(new LoggingLayerOptions
        {
            ProviderName = "github",
            Logger = logger,
        }).Wrap(inner);

        await provider.CreateRepositoryAsync(new CreateRepositoryRequest
        {
            Name = "core",
            Owner = "acme",
            Description = "Bearer super-secret-token",
        });

        Assert.DoesNotContain("super-secret-token", string.Join("|", logger.Events));
    }

    [Fact]
    public async Task LogsFailureWithoutExceptionMessage()
    {
        var logger = new RecordingLogger();
        var inner = Substitute.For<IVcsProvider>();
        inner.RepositoryExistsAsync(Arg.Any<RepositoryRef>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<bool>(
                new VcsProviderException("Authorization: Bearer super-secret-token")));
        var provider = new LoggingLayer(new LoggingLayerOptions
        {
            ProviderName = "github",
            Logger = logger,
        }).Wrap(inner);

        await Assert.ThrowsAsync<VcsProviderException>(
            () => provider.RepositoryExistsAsync(new RepositoryRef { Owner = "acme", Name = "core" }));

        Assert.Contains("failure:RepositoryExists:acme/core::github", logger.Events);
        Assert.Contains("VcsProviderException", logger.FailureTypes);
        Assert.DoesNotContain("super-secret-token", string.Join("|", logger.Events));
        Assert.DoesNotContain("super-secret-token", string.Join("|", logger.FailureTypes));
    }

    private sealed class RecordingLogger : IVcsProviderLogger
    {
        public List<string> Events { get; } = [];

        public List<string> FailureTypes { get; } = [];

        public void LogStart(VcsProviderLogContext context)
        {
            Events.Add(Format(context));
        }

        public void LogSuccess(VcsProviderLogContext context, TimeSpan duration)
        {
            Events.Add(Format(context));
        }

        public void LogFailure(
            VcsProviderLogContext context,
            TimeSpan duration,
            string exceptionType)
        {
            Events.Add(Format(context));
            FailureTypes.Add(exceptionType);
        }

        private static string Format(VcsProviderLogContext context)
        {
            return $"{context.Outcome}:{context.Operation}:{context.Repository}:{context.PullRequestNumber}:{context.ProviderName}";
        }
    }
}
