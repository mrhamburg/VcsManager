namespace VcsManager.Internal;

internal static class DraftTitle
{
    public static bool IsDraft(string? title, params string[] prefixes)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var trimmed = title.TrimStart();
        return prefixes.Any(prefix => trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public static string Apply(
        string title,
        bool draft,
        string prefix,
        params string[] additionalPrefixes)
    {
        string[] prefixes = [prefix, .. additionalPrefixes];
        return draft && !IsDraft(title, prefixes) ? $"{prefix} {title}" : title;
    }
}
