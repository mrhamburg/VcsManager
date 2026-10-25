namespace VcsManager.AzureDevOps.Internal;

internal sealed class AzureDevOpsRepositoryListResponse
{
    public List<AzureDevOpsRepositoryResponse> Value { get; set; } = [];
}

internal sealed class AzureDevOpsRepositoryResponse
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? WebUrl { get; set; }

    public string? DefaultBranch { get; set; }

    public AzureDevOpsProjectResponse? Project { get; set; }
}

internal sealed class AzureDevOpsProjectResponse
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Url { get; set; }
}


internal sealed class AzureDevOpsRefListResponse
{
    public List<AzureDevOpsRefResponse> Value { get; set; } = [];
}

internal sealed class AzureDevOpsRefResponse
{
    public string? Name { get; set; }
}

internal sealed class AzureDevOpsCreatePushRequest
{
    public required IReadOnlyList<AzureDevOpsRefUpdate> RefUpdates { get; set; }

    public required IReadOnlyList<AzureDevOpsCommit> Commits { get; set; }
}

internal sealed class AzureDevOpsRefUpdate
{
    public required string Name { get; set; }

    public required string OldObjectId { get; set; }
}

internal sealed class AzureDevOpsCommit
{
    public required string Comment { get; set; }

    public required IReadOnlyList<AzureDevOpsChange> Changes { get; set; }
}

internal sealed class AzureDevOpsChange
{
    public required string ChangeType { get; set; }

    public required AzureDevOpsItem Item { get; set; }

    public required AzureDevOpsItemContent NewContent { get; set; }
}

internal sealed class AzureDevOpsItem
{
    public required string Path { get; set; }
}

internal sealed class AzureDevOpsItemContent
{
    public required string Content { get; set; }

    public required string ContentType { get; set; }
}
