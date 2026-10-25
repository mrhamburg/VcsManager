using System.Globalization;

namespace VcsManager.Gitea.Internal;

internal static class GiteaMapper
{
    public static VcsRepository ToRepository(GiteaRepository repository)
    {
        return new VcsRepository
        {
            Id = repository.Id.ToString(CultureInfo.InvariantCulture),
            Name = repository.Name,
            Owner = repository.Owner,
            FullName = repository.FullName,
            Url = repository.WebUrl,
            DefaultBranch = repository.DefaultBranch,
            Visibility = repository.Visibility,
            Description = repository.Description,
            IsArchived = repository.IsArchived,
            CreatedAt = repository.CreatedAt,
            UpdatedAt = repository.UpdatedAt,
        };
    }

    public static PullRequest ToPullRequest(GiteaPullRequest pullRequest)
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

    public static GiteaRepository FromRepositoryResponse(GiteaRepositoryResponse response)
    {
        var owner = response.Owner?.Login;
        if (string.IsNullOrWhiteSpace(owner) && response.FullName.Contains('/'))
        {
            owner = response.FullName.Split('/')[0];
        }

        return new GiteaRepository
        {
            Id = response.Id,
            Name = response.Name,
            Owner = owner ?? string.Empty,
            FullName = response.FullName,
            WebUrl = response.HtmlUrl ?? string.Empty,
            DefaultBranch = response.DefaultBranch,
            Visibility = response.Private ? VcsVisibility.Private : VcsVisibility.Public,
            Description = response.Description,
            IsArchived = response.Archived,
            IsEmpty = response.Empty,
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt,
        };
    }

    public static GiteaPullRequest FromPullRequestResponse(GiteaPullRequestResponse response)
    {
        var isDraft = response.Draft || GiteaDraftTitle.IsDraft(response.Title);

        // A merged pull request has state = closed and merged = true. Check the
        // merged flag first so it is not reported as merely closed.
        var state = response.Merged
            ? PullRequestState.Merged
            : string.Equals(response.State, "closed", StringComparison.OrdinalIgnoreCase)
                ? PullRequestState.Closed
                : isDraft
                    ? PullRequestState.Draft
                    : PullRequestState.Open;

        return new GiteaPullRequest
        {
            Id = response.Id,
            Number = checked((int)response.Number),
            Title = response.Title,
            Description = response.Body,
            SourceBranch = response.Head?.Ref ?? string.Empty,
            TargetBranch = response.Base?.Ref ?? string.Empty,
            State = state,
            IsDraft = isDraft,
            Author = response.User?.Login ?? string.Empty,
            WebUrl = response.HtmlUrl ?? string.Empty,
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt,
            MergedAt = response.MergedAt,
            ClosedAt = response.ClosedAt,
        };
    }
}
