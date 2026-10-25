namespace VcsManager.Gitea.Internal;

internal static class GiteaDraftTitle
{
    private const string Prefix = "WIP:";

    public static bool IsDraft(string? title)
    {
        return !string.IsNullOrWhiteSpace(title) &&
            title.TrimStart().StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
    }

    public static string Apply(string title, bool draft)
    {
        return draft && !IsDraft(title) ? $"{Prefix} {title}" : title;
    }
}
