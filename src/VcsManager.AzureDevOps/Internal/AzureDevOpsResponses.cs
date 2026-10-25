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
