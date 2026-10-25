namespace VcsManager.Gitea.Internal;

internal sealed record GiteaPage<T>(
    IReadOnlyList<T> Items,
    bool HasNextPage,
    int? TotalCount);
