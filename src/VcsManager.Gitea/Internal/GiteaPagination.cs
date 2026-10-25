namespace VcsManager.Gitea.Internal;

internal static class GiteaPagination
{
    public const int MaxPageSize = 50;

    public static int EffectivePageSize(int requestedPageSize)
    {
        return Math.Min(requestedPageSize, MaxPageSize);
    }
}
