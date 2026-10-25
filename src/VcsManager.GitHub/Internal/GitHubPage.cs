namespace VcsManager.GitHub.Internal;

internal sealed record GitHubPage<T>(IReadOnlyList<T> Items, bool HasNextPage);
