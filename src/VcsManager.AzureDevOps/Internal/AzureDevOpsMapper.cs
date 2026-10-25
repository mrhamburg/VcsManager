using System.Globalization;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace VcsManager.AzureDevOps.Internal;

internal static class AzureDevOpsMapper
{
    private static DateTimeOffset? ToDateTimeOffset(DateTime value)
    {
        return value == default ? null : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    public static GitPullRequestMergeStrategy ToMergeStrategy(MergeMethod method)
    {
        return method switch
        {
            MergeMethod.Squash => GitPullRequestMergeStrategy.Squash,
            MergeMethod.Rebase => GitPullRequestMergeStrategy.Rebase,
            _ => GitPullRequestMergeStrategy.NoFastForward,
        };
    }

    public static AzureDevOpsPullRequest FromSdkPullRequest(
        GitPullRequest pullRequest,
        string organizationUrl,
        string project,
        string repositoryName)
    {
        var isDraft = pullRequest.IsDraft == true;
        var state = pullRequest.Status switch
        {
            PullRequestStatus.Abandoned => PullRequestState.Closed,
            PullRequestStatus.Completed => PullRequestState.Merged,
            _ => isDraft ? PullRequestState.Draft : PullRequestState.Open,
        };

        var closedAt = ToDateTimeOffset(pullRequest.ClosedDate);
        var webUrl =
            $"{organizationUrl.TrimEnd('/')}/{Uri.EscapeDataString(project)}/_git/{Uri.EscapeDataString(repositoryName)}/pullrequest/{pullRequest.PullRequestId}";

        return new AzureDevOpsPullRequest
        {
            Id = pullRequest.PullRequestId,
            Number = pullRequest.PullRequestId,
            Title = pullRequest.Title ?? string.Empty,
            Description = pullRequest.Description,
            SourceBranch = AzureDevOpsRefs.FromRef(pullRequest.SourceRefName),
            TargetBranch = AzureDevOpsRefs.FromRef(pullRequest.TargetRefName),
            State = state,
            IsDraft = isDraft,
            Author = pullRequest.CreatedBy?.DisplayName ??
                pullRequest.CreatedBy?.UniqueName ??
                string.Empty,
            WebUrl = webUrl,
            SourceCommitId = pullRequest.LastMergeSourceCommit?.CommitId,
            CreatedAt = ToDateTimeOffset(pullRequest.CreationDate),
            MergedAt = state == PullRequestState.Merged ? closedAt : null,
            ClosedAt = state == PullRequestState.Closed ? closedAt : null,
        };
    }

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
