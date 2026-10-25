namespace VcsManager.Gitea.Internal;

internal sealed record GiteaOwner(
    long Id,
    bool IsOrganization);
