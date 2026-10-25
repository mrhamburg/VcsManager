using System.Text.Json.Serialization;

namespace VcsManager.AzureDevOps.Internal;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AzureDevOpsRepositoryListResponse))]
[JsonSerializable(typeof(AzureDevOpsRepositoryResponse))]
[JsonSerializable(typeof(AzureDevOpsProjectResponse))]
[JsonSerializable(typeof(AzureDevOpsRefListResponse))]
[JsonSerializable(typeof(AzureDevOpsRefResponse))]
[JsonSerializable(typeof(AzureDevOpsCreatePushRequest))]
[JsonSerializable(typeof(AzureDevOpsRefUpdate))]
[JsonSerializable(typeof(AzureDevOpsCommit))]
[JsonSerializable(typeof(AzureDevOpsChange))]
[JsonSerializable(typeof(AzureDevOpsItem))]
[JsonSerializable(typeof(AzureDevOpsItemContent))]
internal partial class AzureDevOpsJsonContext : JsonSerializerContext;
