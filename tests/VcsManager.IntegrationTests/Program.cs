using VcsManager.AzureDevOps;
using VcsManager.Builder;
using VcsManager.Gitea;
using VcsManager.GitHub;
using VcsManager.GitLab;

namespace VcsManager.IntegrationTests;

internal static class Program
{
    private const string GitLabUrlVariable = "VCS_GITLAB_TEST_URL";
    private const string GitLabTokenVariable = "VCS_GITLAB_TEST_TOKEN";
    private const string GitLabNamespaceVariable = "VCS_GITLAB_TEST_NAMESPACE";
    private const string GitLabArgument = "--gitlab";

    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        if (args.Length == 1 && string.Equals(args[0], GitLabArgument, StringComparison.Ordinal))
        {
            return await RunGitLabFromEnvironmentAsync();
        }

        if (args.Length != 1)
        {
            PrintUsage();
            return 1;
        }

        var configPath = args[0];
        if (!File.Exists(configPath))
        {
            WriteError($"Config file not found: {configPath}");
            return 1;
        }

        VcsProviderConfiguration configuration;
        try
        {
            configuration = YamlConfigurationReader.Read(File.ReadAllText(configPath));
        }
        catch (Exception exception)
        {
            WriteError($"Failed to read config: {exception.Message}");
            return 1;
        }

        Console.WriteLine($"Loading config from: {configPath}");
        return await RunAsync(configuration);
    }

    private static async Task<int> RunGitLabFromEnvironmentAsync()
    {
        var url = ReadEnvironmentVariable(GitLabUrlVariable);
        var token = ReadEnvironmentVariable(GitLabTokenVariable);
        var @namespace = ReadEnvironmentVariable(GitLabNamespaceVariable);

        if (url is null || token is null || @namespace is null)
        {
            Console.WriteLine(
                $"SKIP: Set {GitLabUrlVariable}, {GitLabTokenVariable}, and " +
                $"{GitLabNamespaceVariable} to run the GitLab integration tests.");
            return 0;
        }

        var configuration = new VcsProviderConfiguration
        {
            ProviderName = "gitlab",
            Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["base.url"] = url,
                ["token"] = token,
                ["default.namespace"] = @namespace,
            },
        };

        Console.WriteLine($"GitLab URL: {url}");
        Console.WriteLine($"GitLab namespace: {@namespace}");
        return await RunAsync(configuration);
    }

    private static async Task<int> RunAsync(VcsProviderConfiguration configuration)
    {
        Console.WriteLine($"Provider: {configuration.ProviderName}");
        Console.WriteLine();

        IVcsProvider provider;
        try
        {
            provider = CreateProvider(configuration);
        }
        catch (Exception exception)
        {
            WriteError($"Failed to build provider: {exception.Message}");
            return 1;
        }

        await using (provider)
        {
            var scenario = new IntegrationScenario(provider, configuration);
            return await scenario.RunAsync();
        }
    }

    private static string? ReadEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IVcsProvider CreateProvider(VcsProviderConfiguration configuration)
    {
        var builder = new VcsProviderBuilder()
            .WithGitHub()
            .WithGitLab()
            .WithAzureDevOps()
            .WithGitea();

        builder.UseProvider(configuration.ProviderName, settings =>
        {
            foreach (var setting in configuration.Settings)
            {
                settings[setting.Key] = setting.Value;
            }
        });

        return builder.Build();
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: VcsManager.IntegrationTests <config.yaml>");
        Console.WriteLine("       VcsManager.IntegrationTests --gitlab");
        Console.WriteLine();
        Console.WriteLine("Runs the full repository and pull request lifecycle against the");
        Console.WriteLine("provider configured in the YAML file.");
        Console.WriteLine();
        Console.WriteLine("With --gitlab, reads VCS_GITLAB_TEST_URL, VCS_GITLAB_TEST_TOKEN,");
        Console.WriteLine("and VCS_GITLAB_TEST_NAMESPACE from the environment. The GitLab run");
        Console.WriteLine("is skipped when any of these variables is missing.");
        Console.WriteLine();
        Console.WriteLine("Sample configs are in the configs/ directory.");
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }
}
