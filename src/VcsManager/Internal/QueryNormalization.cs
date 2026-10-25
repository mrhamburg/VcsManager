namespace VcsManager.Internal;

internal static class QueryNormalization
{
    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static RepositoryQuery Normalize(RepositoryQuery query)
    {
        return query with
        {
            Owner = NormalizeOptional(query.Owner),
            Project = NormalizeOptional(query.Project),
            NameContains = NormalizeOptional(query.NameContains),
        };
    }

    public static PullRequestQuery Normalize(PullRequestQuery query)
    {
        return query with
        {
            Author = NormalizeOptional(query.Author),
            SourceBranch = NormalizeOptional(query.SourceBranch),
            TargetBranch = NormalizeOptional(query.TargetBranch),
        };
    }
}
