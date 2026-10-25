using System.Text.Json.Serialization;

namespace VcsManager.AzureDevOps.Internal;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AzureDevOpsRepositoryListResponse))]
[JsonSerializable(typeof(AzureDevOpsRepositoryResponse))]
[JsonSerializable(typeof(AzureDevOpsProjectResponse))]
internal partial class AzureDevOpsJsonContext : JsonSerializerContext;
