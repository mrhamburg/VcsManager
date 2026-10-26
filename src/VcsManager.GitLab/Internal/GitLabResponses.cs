using System.Text.Json.Serialization;

namespace VcsManager.GitLab.Internal;

internal sealed class GitLabCreateProjectRequest
{
    public required string Name { get; set; }

    public required string Path { get; set; }

    public string? Description { get; set; }

    public required string Visibility { get; set; }

    public bool InitializeWithReadme { get; set; }

    public string? DefaultBranch { get; set; }

    public long? NamespaceId { get; set; }
}

internal sealed class GitLabCreateCommitRequest
{
    public required string Branch { get; set; }

    public required string CommitMessage { get; set; }

    public required IReadOnlyList<GitLabCommitAction> Actions { get; set; }
}

internal sealed class GitLabCommitAction
{
    public required string Action { get; set; }

    public required string FilePath { get; set; }

    public required string Content { get; set; }

    public required string Encoding { get; set; }
}

internal sealed class GitLabUpdateProjectRequest
{
    public required string DefaultBranch { get; set; }
}

internal sealed class GitLabCreateMergeRequestRequest
{
    public required string SourceBranch { get; set; }

    public required string TargetBranch { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }
}

internal sealed class GitLabUpdateMergeRequestRequest
{
    public string? StateEvent { get; set; }
}

internal sealed class GitLabMergeRequestMergeRequest
{
    public string? MergeCommitMessage { get; set; }

    public string? SquashCommitMessage { get; set; }

    public bool? ShouldRemoveSourceBranch { get; set; }

    public bool? Squash { get; set; }
}

internal sealed class GitLabProjectResponse
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string PathWithNamespace { get; set; } = string.Empty;

    public string WebUrl { get; set; } = string.Empty;

    public string? DefaultBranch { get; set; }

    public string? Visibility { get; set; }

    public string? Description { get; set; }

    public bool Archived { get; set; }

    public bool EmptyRepo { get; set; }

    public string? MarkedForDeletionAt { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public GitLabNamespaceResponse? Namespace { get; set; }
}

internal sealed class GitLabNamespaceResponse
{
    public long Id { get; set; }

    public string Path { get; set; } = string.Empty;
}

internal sealed class GitLabMergeRequestResponse
{
    public long Id { get; set; }

    public int Iid { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string SourceBranch { get; set; } = string.Empty;

    public string TargetBranch { get; set; } = string.Empty;

    public string? State { get; set; }

    public bool Draft { get; set; }

    [JsonPropertyName("work_in_progress")]
    public bool WorkInProgress { get; set; }

    public GitLabUserResponse? Author { get; set; }

    public string WebUrl { get; set; } = string.Empty;

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? MergedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }
}

internal sealed class GitLabUserResponse
{
    public string Username { get; set; } = string.Empty;
}
