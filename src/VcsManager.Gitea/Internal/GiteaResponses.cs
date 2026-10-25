using System.Text.Json.Serialization;

namespace VcsManager.Gitea.Internal;

internal sealed class GiteaCreateRepositoryRequest
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool Private { get; set; }

    public bool AutoInit { get; set; }

    public string? DefaultBranch { get; set; }
}

internal sealed class GiteaChangeFilesRequest
{
    public required string Message { get; set; }

    public required string Branch { get; set; }

    public required IReadOnlyList<GiteaChangeFileOperation> Files { get; set; }
}

internal sealed class GiteaChangeFileOperation
{
    public required string Operation { get; set; }

    public required string Path { get; set; }

    public required string Content { get; set; }
}

internal sealed class GiteaCreatePullRequestRequest
{
    public required string Head { get; set; }

    public required string Base { get; set; }

    public required string Title { get; set; }

    public string? Body { get; set; }
}

internal sealed class GiteaEditPullRequestRequest
{
    public string? State { get; set; }
}

internal sealed class GiteaMergePullRequestRequest
{
    public required string Do { get; set; }

    public bool? DeleteBranchAfterMerge { get; set; }

    public string? MergeTitleField { get; set; }

    public string? MergeMessageField { get; set; }
}

internal sealed class GiteaRepositoryResponse
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? HtmlUrl { get; set; }

    public string? DefaultBranch { get; set; }

    public string? Description { get; set; }

    public bool Private { get; set; }

    public bool Internal { get; set; }

    public bool Archived { get; set; }

    public bool Empty { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public GiteaUserResponse? Owner { get; set; }
}

internal sealed class GiteaUserResponse
{
    public long Id { get; set; }

    public string Login { get; set; } = string.Empty;
}

internal sealed class GiteaOrganizationResponse
{
    public long Id { get; set; }
}

internal sealed class GiteaPullRequestResponse
{
    public long Id { get; set; }

    public long Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }

    public string? State { get; set; }

    public bool Draft { get; set; }

    public bool Merged { get; set; }

    public string? HtmlUrl { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? MergedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public GiteaUserResponse? User { get; set; }

    public GiteaPullRequestBranchResponse? Head { get; set; }

    public GiteaPullRequestBranchResponse? Base { get; set; }
}

internal sealed class GiteaPullRequestBranchResponse
{
    [JsonPropertyName("ref")]
    public string? Ref { get; set; }
}

internal sealed class GiteaSearchResponse<T>
{
    public bool Ok { get; set; }

    public List<T> Data { get; set; } = [];
}
