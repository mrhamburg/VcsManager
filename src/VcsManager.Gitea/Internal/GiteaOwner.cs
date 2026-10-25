namespace VcsManager.Gitea.Internal;

internal sealed record GiteaOwner(
    long Id,
    string Login,
    bool IsOrganization);
