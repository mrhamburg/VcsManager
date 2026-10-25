using System.Text.Json.Serialization;

namespace VcsManager.Gitea.Internal;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(GiteaCreateRepositoryRequest))]
[JsonSerializable(typeof(GiteaCreatePullRequestRequest))]
[JsonSerializable(typeof(GiteaEditPullRequestRequest))]
[JsonSerializable(typeof(GiteaMergePullRequestRequest))]
[JsonSerializable(typeof(GiteaRepositoryResponse))]
[JsonSerializable(typeof(GiteaUserResponse))]
[JsonSerializable(typeof(GiteaOrganizationResponse))]
[JsonSerializable(typeof(GiteaPullRequestResponse))]
[JsonSerializable(typeof(GiteaSearchResponse<GiteaRepositoryResponse>))]
[JsonSerializable(typeof(List<GiteaRepositoryResponse>))]
[JsonSerializable(typeof(List<GiteaPullRequestResponse>))]
internal partial class GiteaJsonContext : JsonSerializerContext;
