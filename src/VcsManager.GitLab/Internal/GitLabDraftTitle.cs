namespace VcsManager.GitLab.Internal;

internal static class GitLabDraftTitle
{
    private static readonly string[] Prefixes = ["Draft:", "WIP:"];

    public static bool IsDraft(string? title)
    {
        return !string.IsNullOrWhiteSpace(title) &&
            Prefixes.Any(prefix => title.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public static string Apply(string title, bool draft)
    {
        return draft && !IsDraft(title) ? $"Draft: {title}" : title;
    }
}
