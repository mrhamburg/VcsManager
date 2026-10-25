namespace VcsManager.Internal;

internal static class QueryMatcher
{
    public static bool Matches(
        PullRequestQuery query,
        PullRequestState? state,
        string author,
        string sourceBranch,
        string targetBranch)
    {
        if (query.State is { } expectedState && state != expectedState)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Author) &&
            !string.Equals(author, query.Author, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.SourceBranch) &&
            !string.Equals(sourceBranch, query.SourceBranch, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.TargetBranch) &&
            !string.Equals(targetBranch, query.TargetBranch, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }
}
