namespace VcsManager.GitLab.Internal;

internal static class GitLabProjectPath
{
    public static string From(RepositoryRef repository, string? defaultNamespace)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var owner = ResolveOwner(repository.Owner, defaultNamespace);
        return Combine(owner, repository.Project, repository.Name);
    }

    public static string From(CreateRepositoryRequest request, string? defaultNamespace)
    {
        ArgumentNullException.ThrowIfNull(request);

        var owner = ResolveOwner(request.Owner, defaultNamespace);
        return Combine(owner, request.Project, request.Name);
    }

    public static string Combine(string? owner, string? project, string name)
    {
        var segments = new List<string>(3);

        AddSegment(segments, owner);
        AddSegment(segments, project);
        AddSegment(segments, name);

        return string.Join("/", segments);
    }

    public static string CombineNamespace(string? owner, string? project)
    {
        var segments = new List<string>(2);

        AddSegment(segments, owner);
        AddSegment(segments, project);

        if (segments.Count == 0)
        {
            throw new ArgumentException(
                "A GitLab namespace is required. Set Owner or GitLabOptions.DefaultNamespace.",
                nameof(owner));
        }

        return string.Join("/", segments);
    }

    public static string Encode(string path)
    {
        return Uri.EscapeDataString(path.Trim());
    }

    private static string ResolveOwner(string? owner, string? defaultNamespace)
    {
        var resolved = string.IsNullOrWhiteSpace(owner) ? defaultNamespace : owner;

        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new ArgumentException(
                "A GitLab namespace is required. Set Owner or GitLabOptions.DefaultNamespace.",
                nameof(owner));
        }

        return resolved.Trim();
    }

    private static void AddSegment(List<string> segments, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            segments.Add(value.Trim());
        }
    }
}
