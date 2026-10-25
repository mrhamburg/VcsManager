namespace VcsManager.AzureDevOps.Internal;

internal static class AzureDevOpsRefs
{
    private const string BranchPrefix = "refs/heads/";

    public static string ToRef(string branch)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(branch);

        var value = branch.Trim();
        return value.StartsWith(BranchPrefix, StringComparison.OrdinalIgnoreCase)
            ? value
            : BranchPrefix + value;
    }

    public static string FromRef(string? refName)
    {
        if (string.IsNullOrWhiteSpace(refName))
        {
            return string.Empty;
        }

        var value = refName.Trim();
        return value.StartsWith(BranchPrefix, StringComparison.OrdinalIgnoreCase)
            ? value[BranchPrefix.Length..]
            : value;
    }
}
