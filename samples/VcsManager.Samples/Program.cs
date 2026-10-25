using VcsManager;
using VcsManager.Builder;
using VcsManager.Samples;

return await SampleProgram.RunAsync(args);

internal static class SampleProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        VcsProviderBuilder builder;
        try
        {
            builder = CreateBuilder(args);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or IOException)
        {
            PrintConfigurationError(exception);
            return 1;
        }

        IVcsProvider provider;
        try
        {
            provider = builder.Build();
        }
        catch (InvalidOperationException exception)
        {
            PrintConfigurationError(exception);
            return 1;
        }

        try
        {
            await using (provider)
            {
                await RunFlowAsync(provider);
            }

            return 0;
        }
        catch (VcsProviderException exception)
        {
            Console.Error.WriteLine($"Provider error: {exception.Message}");
            return 2;
        }
    }

    private static VcsProviderBuilder CreateBuilder(string[] args)
    {
        var builder = new VcsProviderBuilder()
            .RegisterProvider(new DemoVcsProviderFactory());

        if (args.Length == 2 && args[0] == "--config")
        {
            builder.FromYamlFile(args[1]);
            return builder;
        }

        if (args.Length != 0)
        {
            throw new ArgumentException("Usage: VcsManager.Samples [--config <path>]");
        }

        var configFile = Environment.GetEnvironmentVariable("VCS_CONFIG_FILE");
        if (!string.IsNullOrWhiteSpace(configFile))
        {
            builder.FromYamlFile(configFile);
            return builder;
        }

        var yaml = Environment.GetEnvironmentVariable("VCS_CONFIG");
        if (!string.IsNullOrWhiteSpace(yaml))
        {
            builder.FromYaml(yaml);
            return builder;
        }

        builder.FromEnvironment("VCS");
        return builder;
    }

    private static async Task RunFlowAsync(IVcsProvider provider)
    {
        var repositoryName =
            $"vcsmanager-sample-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..48];
        var repository = new RepositoryRef
        {
            Name = repositoryName,
            Owner = ResolveOwner(),
        };

        var createdRepository = await provider.CreateRepositoryAsync(new CreateRepositoryRequest
        {
            Name = repository.Name,
            Owner = repository.Owner,
            DefaultBranch = "main",
        });
        Console.WriteLine($"Created repository: {createdRepository.FullName}");

        var repositoryPage = await provider.ListRepositoriesAsync(
            new RepositoryQuery { NameContains = repositoryName },
            new PaginationOptions { PageSize = 10 });
        Console.WriteLine("Repositories:");
        foreach (var item in repositoryPage.Items)
        {
            Console.WriteLine($"  - {item.FullName}");
        }

        var first = await provider.CreatePullRequestAsync(repository, new CreatePullRequestRequest
        {
            Title = "Sample: merge a pull request",
            SourceBranch = "sample/merge",
            TargetBranch = "main",
        });
        Console.WriteLine($"Created pull request #{first.Number}: {first.State}");

        var merged = await provider.MergePullRequestAsync(repository, first.Number);
        Console.WriteLine($"Merged pull request #{merged.Number}: {merged.State}");

        var second = await provider.CreatePullRequestAsync(repository, new CreatePullRequestRequest
        {
            Title = "Sample: close a pull request",
            SourceBranch = "sample/close",
            TargetBranch = "main",
        });
        Console.WriteLine($"Created pull request #{second.Number}: {second.State}");

        var closed = await provider.ClosePullRequestAsync(repository, second.Number);
        Console.WriteLine($"Closed pull request #{closed.Number}: {closed.State}");

        await provider.DeleteRepositoryAsync(repository);
        Console.WriteLine($"Deleted repository: {createdRepository.FullName}");
    }

    private static string? ResolveOwner()
    {
        var providerName = Environment.GetEnvironmentVariable("VCS_PROVIDER");
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return null;
        }

        var owner = Environment.GetEnvironmentVariable($"VCS_{providerName.ToUpperInvariant()}_OWNER");
        return string.IsNullOrWhiteSpace(owner) ? null : owner.Trim();
    }

    private static void PrintConfigurationError(Exception exception)
    {
        Console.Error.WriteLine($"Configuration error: {exception.Message}");
        Console.Error.WriteLine(
            "Set VCS_PROVIDER=demo and optionally VCS_DEMO_OWNER, " +
            "or set VCS_CONFIG_FILE or VCS_CONFIG.");
    }
}
