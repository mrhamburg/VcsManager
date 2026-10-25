using System.Globalization;

namespace VcsManager.AzureDevOps.Internal;

internal static class AzureDevOpsMapper
{
    public static VcsRepository ToRepository(AzureDevOpsRepository repository)
    {
        return new VcsRepository
        {
            Id = repository.Id.ToString("D", CultureInfo.InvariantCulture),
            Name = repository.Name,
            Owner = repository.Organization,
            Project = repository.Project,
            FullName = $"{repository.Organization}/{repository.Project}/{repository.Name}",
            Url = repository.WebUrl,
            DefaultBranch = AzureDevOpsRefs.FromRef(repository.DefaultBranch) is { Length: > 0 } branch
                ? branch
                : null,
            Visibility = VcsVisibility.Private,
            CreatedAt = repository.CreatedAt,
        };
    }

    public static PullRequest ToPullRequest(AzureDevOpsPullRequest pullRequest)
    {
        return new PullRequest
        {
            Id = pullRequest.Id.ToString(CultureInfo.InvariantCulture),
            Number = pullRequest.Number,
            Title = pullRequest.Title,
            Description = pullRequest.Description,
            SourceBranch = pullRequest.SourceBranch,
            TargetBranch = pullRequest.TargetBranch,
            State = pullRequest.State,
            IsDraft = pullRequest.IsDraft,
            Author = pullRequest.Author,
            Url = pullRequest.WebUrl,
            CreatedAt = pullRequest.CreatedAt,
            UpdatedAt = pullRequest.UpdatedAt,
            MergedAt = pullRequest.MergedAt,
            ClosedAt = pullRequest.ClosedAt,
        };
    }
}
