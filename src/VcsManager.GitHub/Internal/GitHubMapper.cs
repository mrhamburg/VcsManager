namespace VcsManager.GitHub.Internal;

internal static class GitHubMapper
{
    public static VcsRepository ToRepository(GitHubRepository repository)
    {
        return new VcsRepository
        {
            Id = repository.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Name = repository.Name,
            Owner = repository.Owner,
            FullName = repository.FullName,
            Url = repository.HtmlUrl,
            DefaultBranch = repository.DefaultBranch,
            Visibility = repository.Visibility,
            Description = repository.Description,
            IsArchived = repository.IsArchived,
            CreatedAt = repository.CreatedAt,
            UpdatedAt = repository.UpdatedAt,
        };
    }

    public static PullRequest ToPullRequest(GitHubPullRequest pullRequest)
    {
        return new PullRequest
        {
            Id = pullRequest.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Number = pullRequest.Number,
            Title = pullRequest.Title,
            Description = pullRequest.Description,
            SourceBranch = pullRequest.SourceBranch,
            TargetBranch = pullRequest.TargetBranch,
            State = pullRequest.State,
            IsDraft = pullRequest.IsDraft,
            Author = pullRequest.Author,
            Url = pullRequest.HtmlUrl,
            CreatedAt = pullRequest.CreatedAt,
            UpdatedAt = pullRequest.UpdatedAt,
            MergedAt = pullRequest.MergedAt,
            ClosedAt = pullRequest.ClosedAt,
        };
    }
}
