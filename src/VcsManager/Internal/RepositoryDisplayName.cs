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
        return Format(repository.Owner, repository.Project, repository.Name);
    }
}
