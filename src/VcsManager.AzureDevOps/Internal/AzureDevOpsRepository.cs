namespace VcsManager.AzureDevOps.Internal;

internal sealed record AzureDevOpsRepository
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Project { get; init; }

    public required string Organization { get; init; }

    public required string WebUrl { get; init; }

    public string? DefaultBranch { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
}
