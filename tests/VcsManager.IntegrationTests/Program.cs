using VcsManager.AzureDevOps;
using VcsManager.Builder;
using VcsManager.Gitea;
using VcsManager.GitHub;
using VcsManager.GitLab;

namespace VcsManager.IntegrationTests;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
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
        Console.WriteLine();
        Console.WriteLine("Runs the full repository and pull request lifecycle against the");
        Console.WriteLine("provider configured in the YAML file.");
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
