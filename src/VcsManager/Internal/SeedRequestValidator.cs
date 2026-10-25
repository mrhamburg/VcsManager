namespace VcsManager.Internal;

internal static class SeedRequestValidator
{
    public const string DefaultCommitMessage = "Initial commit";

    public static SeedRepositoryRequest Normalize(SeedRepositoryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var branch = request.Branch?.Trim();
        if (string.IsNullOrWhiteSpace(branch))
        {
            throw new ArgumentException(
                "A branch is required for repository seeding.",
                nameof(request));
        }

        if (request.Files is null || request.Files.Count == 0)
        {
            throw new ArgumentException(
                "At least one file is required for repository seeding.",
                nameof(request));
        }

        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in request.Files)
        {
            ArgumentNullException.ThrowIfNull(file);
            ValidatePath(file.Path, nameof(request));
            if (!paths.Add(file.Path))
            {
                throw new ArgumentException(
                    $"The seed contains duplicate path '{file.Path}'.",
                    nameof(request));
            }
        }

        var commitMessage = string.IsNullOrWhiteSpace(request.CommitMessage)
            ? DefaultCommitMessage
            : request.CommitMessage.Trim();

        return request with
        {
            Branch = branch,
            CommitMessage = commitMessage,
        };
    }

    private static void ValidatePath(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "A seed file path is required.",
                parameterName);
        }

        if (path[0] == '/' ||
            path[^1] == '/' ||
            path.Contains('\\', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The seed file path '{path}' must be relative and use forward slashes.",
                parameterName);
        }

        var segments = path.Split('/');
        foreach (var segment in segments)
        {
            if (segment.Length == 0 ||
                segment == "." ||
                segment == "..")
            {
                throw new ArgumentException(
                    $"The seed file path '{path}' contains an invalid segment.",
                    parameterName);
            }

            if (segment.Equals(".git", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"The seed file path '{path}' must not contain a .git segment.",
                    parameterName);
            }
        }
    }
}
