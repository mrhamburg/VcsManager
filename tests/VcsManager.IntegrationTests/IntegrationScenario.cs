using VcsManager.Builder;

namespace VcsManager.IntegrationTests;

internal sealed class IntegrationScenario
{
    private const string MergeBranch = "integration/merge";
    private const string CloseBranch = "integration/close";

    private readonly IVcsProvider _provider;
    private readonly VcsProviderConfiguration _configuration;
    private string? _repositoryPath;
    private string _repositoryState = "not created";
    private int _passed;
    private int _failed;

    public IntegrationScenario(IVcsProvider provider, VcsProviderConfiguration configuration)
    {
        _provider = provider;
        _configuration = configuration;
    }

    public async Task<int> RunAsync()
    {
        var repositoryName = CreateRepositoryName();
        VcsRepository? created = null;
        RepositoryRef? repository = null;
        var deleted = false;

        try
        {
            await RunStepAsync("Create repository", async () =>
            {
                created = await _provider.CreateRepositoryAsync(new CreateRepositoryRequest
                {
                    Name = repositoryName,
                    Visibility = VcsVisibility.Private,
                    InitializeWithReadme = false,
                });

                Assert(created.Name == repositoryName, $"Expected repository '{repositoryName}', got '{created.Name}'.");
                Assert(created.Visibility == VcsVisibility.Private, $"Expected private visibility, got '{created.Visibility}'.");
                _repositoryPath = created.FullName;
                _repositoryState = "created";
            });

            if (created is null)
            {
                WriteError("Repository was not created. Remaining steps cannot run.");
                return Complete();
            }

            repository = new RepositoryRef
            {
                Owner = created.Owner,
                Project = created.Project,
                Name = created.Name,
            };

            var reference = repository;
            var defaultBranch = created.DefaultBranch ?? "main";

            await RunStepAsync("Get repository", async () =>
            {
                var result = await _provider.GetRepositoryAsync(reference);
                Assert(result.Name == repositoryName, $"Expected repository '{repositoryName}', got '{result.Name}'.");
            });

            await RunStepAsync("Repository exists", async () =>
            {
                var exists = await RetryAsync(
                    () => _provider.RepositoryExistsAsync(reference),
                    value => value);

                Assert(exists, "Expected the repository to exist.");
            });

            await RunStepAsync("List repositories", async () =>
            {
                var page = await RetryAsync(
                    () => _provider.ListRepositoriesAsync(
                        new RepositoryQuery
                        {
                            Owner = reference.Owner,
                            Project = reference.Project,
                            NameContains = repositoryName,
                        },
                        new PaginationOptions { PageSize = 100 }),
                    result => result.Items.Any(item => item.Name == repositoryName));

                Assert(
                    page.Items.Any(item => item.Name == repositoryName),
                    $"Expected the repository list to contain '{repositoryName}'.");
            });

            await RunStepAsync("Seed repository", async () =>
            {
                await _provider.SeedRepositoryAsync(
                    reference,
                    new SeedRepositoryRequest
                    {
                        Branch = defaultBranch,
                        Files =
                        [
                            new RepositorySeedFile
                            {
                                Path = "README.md",
                                Content = "VcsManager integration test repository."u8.ToArray(),
                            },
                        ],
                    });
            });

            using var seeder = new ProviderBranchSeeder(_configuration, created);

            await RunStepAsync($"Seed branch '{MergeBranch}'", async () =>
            {
                await seeder.SeedAsync(MergeBranch);
            });

            PullRequest? mergePullRequest = null;

            await RunStepAsync("Create pull request", async () =>
            {
                mergePullRequest = await _provider.CreatePullRequestAsync(
                    reference,
                    new CreatePullRequestRequest
                    {
                        Title = "Integration test: merge",
                        SourceBranch = MergeBranch,
                        TargetBranch = defaultBranch,
                    });

                Assert(
                    mergePullRequest.State == PullRequestState.Open,
                    $"Expected an open pull request, got '{mergePullRequest.State}'.");
                Assert(
                    mergePullRequest.SourceBranch == MergeBranch,
                    $"Expected source branch '{MergeBranch}', got '{mergePullRequest.SourceBranch}'.");
            });

            if (mergePullRequest is null)
            {
                WriteError("Pull request was not created. Remaining pull request steps cannot run.");
                return Complete();
            }

            await RunStepAsync("Get pull request", async () =>
            {
                var result = await _provider.GetPullRequestAsync(reference, mergePullRequest.Number);
                Assert(result.Number == mergePullRequest.Number, "Expected the same pull request number.");
            });

            await RunStepAsync("List pull requests", async () =>
            {
                var page = await RetryAsync(
                    () => _provider.ListPullRequestsAsync(
                        reference,
                        new PullRequestQuery { SourceBranch = MergeBranch },
                        new PaginationOptions { PageSize = 10 }),
                    result => result.Items.Any(item => item.Number == mergePullRequest.Number));

                Assert(
                    page.Items.Any(item => item.Number == mergePullRequest.Number),
                    $"Expected the pull request list to contain #{mergePullRequest.Number}.");
            });

            await RunStepAsync("Merge pull request", async () =>
            {
                var merged = await RetryOnExceptionAsync(
                    () => _provider.MergePullRequestAsync(
                        reference,
                        mergePullRequest.Number,
                        new MergePullRequestRequest { Method = MergeMethod.Merge }),
                    result => result.State == PullRequestState.Merged);

                Assert(
                    merged.State == PullRequestState.Merged,
                    $"Expected a merged pull request, got '{merged.State}'.");
            });

            await RunStepAsync($"Seed branch '{CloseBranch}'", async () =>
            {
                await seeder.SeedAsync(CloseBranch);
            });

            PullRequest? closePullRequest = null;

            await RunStepAsync("Create pull request to close", async () =>
            {
                closePullRequest = await _provider.CreatePullRequestAsync(
                    reference,
                    new CreatePullRequestRequest
                    {
                        Title = "Integration test: close",
                        SourceBranch = CloseBranch,
                        TargetBranch = defaultBranch,
                    });

                Assert(
                    closePullRequest.State == PullRequestState.Open,
                    $"Expected an open pull request, got '{closePullRequest.State}'.");
            });

            if (closePullRequest is null)
            {
                WriteError("Pull request was not created. Remaining close steps cannot run.");
                return Complete();
            }

            await RunStepAsync("Close pull request", async () =>
            {
                var closed = await _provider.ClosePullRequestAsync(reference, closePullRequest.Number);
                Assert(
                    closed.State == PullRequestState.Closed,
                    $"Expected a closed pull request, got '{closed.State}'.");
            });

            await RunStepAsync("Close pull request again", async () =>
            {
                var closed = await _provider.ClosePullRequestAsync(reference, closePullRequest.Number);
                Assert(
                    closed.State == PullRequestState.Closed,
                    $"Expected the pull request to stay closed, got '{closed.State}'.");
            });

            await RunStepAsync("Delete repository", async () =>
            {
                await _provider.DeleteRepositoryAsync(reference);
                deleted = true;
                _repositoryState = "deleted";
            });

            await RunStepAsync("Repository is deleted", async () =>
            {
                var exists = await RetryAsync(
                    () => _provider.RepositoryExistsAsync(reference),
                    value => !value);

                Assert(!exists, "Expected the repository to be gone.");
            });
        }
        finally
        {
            if (repository is not null && !deleted)
            {
                try
                {
                    await _provider.DeleteRepositoryAsync(repository);
                    _repositoryState = "deleted";
                    Console.WriteLine($"Cleaned up repository: {_repositoryPath}");
                }
                catch (Exception exception)
                {
                    _repositoryState = "cleanup failed";
                    WriteError($"Cleanup failed: {exception.Message}");
                }
            }
        }

        return Complete();
    }

    private async Task RunStepAsync(string name, Func<Task> action)
    {
        Console.Write($"  [{_passed + _failed + 1}] {name}... ");

        try
        {
            await action();
            WriteSuccess("PASS");
            _passed++;
        }
        catch (Exception exception)
        {
            WriteError($"FAIL - {exception.Message}");
            _failed++;
        }
    }

    private int Complete()
    {
        Console.WriteLine();
        Console.WriteLine(new string('-', 50));
        Console.WriteLine($"Repository: {_repositoryPath ?? "(none)"}");
        Console.WriteLine($"Final state: {_repositoryState}");
        Console.WriteLine();

        if (_failed == 0)
        {
            WriteSuccess($"All {_passed} steps passed!");
            return 0;
        }

        WriteError($"{_failed} of {_passed + _failed} steps failed.");
        return 1;
    }

    private static async Task<T> RetryOnExceptionAsync<T>(Func<Task<T>> action, Func<T, bool> accept)
    {
        const int attempts = 10;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var result = await action();
                if (accept(result) || attempt >= attempts)
                {
                    return result;
                }
            }
            catch when (attempt < attempts)
            {
                // The provider can reject the operation while it is still settling.
            }

            await Task.Delay(delay);
        }
    }

    private static async Task<T> RetryAsync<T>(Func<Task<T>> action, Func<T, bool> accept)
    {
        const int attempts = 5;
        var delay = TimeSpan.FromSeconds(2);
        var result = default(T)!;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            result = await action();
            if (accept(result))
            {
                return result;
            }

            if (attempt < attempts)
            {
                await Task.Delay(delay);
            }
        }

        return result;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static string CreateRepositoryName()
    {
        var name = $"vcsmanager-it-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        return name[..40];
    }

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
