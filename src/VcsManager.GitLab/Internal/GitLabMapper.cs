using System.Globalization;

namespace VcsManager.GitLab.Internal;

internal static class GitLabMapper
{
    public static VcsRepository ToRepository(GitLabProject project)
    {
        return new VcsRepository
        {
            Id = project.Id.ToString(CultureInfo.InvariantCulture),
            Name = project.Path,
            Owner = project.Owner,
            Project = project.Project,
            FullName = project.PathWithNamespace,
            Url = project.WebUrl,
            DefaultBranch = project.DefaultBranch,
            Visibility = project.Visibility,
            Description = project.Description,
            IsArchived = project.IsArchived,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
        };
    }

    public static PullRequest ToPullRequest(GitLabMergeRequest mergeRequest)
    {
        return new PullRequest
        {
            Id = mergeRequest.Id.ToString(CultureInfo.InvariantCulture),
            Number = mergeRequest.Iid,
            Title = mergeRequest.Title,
            Description = mergeRequest.Description,
            SourceBranch = mergeRequest.SourceBranch,
            TargetBranch = mergeRequest.TargetBranch,
            State = mergeRequest.State,
            IsDraft = mergeRequest.IsDraft,
            Author = mergeRequest.Author,
            Url = mergeRequest.WebUrl,
            CreatedAt = mergeRequest.CreatedAt,
            UpdatedAt = mergeRequest.UpdatedAt,
            MergedAt = mergeRequest.MergedAt,
            ClosedAt = mergeRequest.ClosedAt,
        };
    }
}
