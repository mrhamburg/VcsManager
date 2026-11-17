namespace VcsManager.Internal;

internal static class RepositoryDisplayName
{
    public static string Format(string? owner, string? project, string name)
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(owner))
        {
            parts.Add(owner);
        }

        if (!string.IsNullOrWhiteSpace(project))
        {
            parts.Add(project);
        }

        parts.Add(name);
        return string.Join("/", parts);
    }

    public static string Format(RepositoryRef repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return Format(repository.Owner, repository.Project, repository.Name);
    }

    public static string? FormatScope(string? owner, string? project)
    {
        if (string.IsNullOrWhiteSpace(owner) && string.IsNullOrWhiteSpace(project))
        {
            return null;
        }

        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(owner))
        {
            parts.Add(owner);
        }

        if (!string.IsNullOrWhiteSpace(project))
        {
            parts.Add(project);
        }

        return string.Join("/", parts);
    }
}
