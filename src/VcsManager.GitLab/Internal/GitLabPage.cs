namespace VcsManager.GitLab.Internal;

internal sealed record GitLabPage<T>(
    IReadOnlyList<T> Items,
    bool HasNextPage,
    int? TotalCount);
