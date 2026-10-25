namespace VcsManager.AzureDevOps.Internal;

internal sealed record AzureDevOpsPage<T>(
    IReadOnlyList<T> Items,
    bool HasNextPage,
    int? TotalCount);
