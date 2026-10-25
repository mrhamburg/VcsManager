using System.Text.Json.Serialization;

namespace VcsManager.GitLab.Internal;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(GitLabCreateProjectRequest))]
[JsonSerializable(typeof(GitLabCreateCommitRequest))]
[JsonSerializable(typeof(GitLabCommitAction))]
[JsonSerializable(typeof(GitLabUpdateProjectRequest))]
[JsonSerializable(typeof(GitLabCreateMergeRequestRequest))]
[JsonSerializable(typeof(GitLabUpdateMergeRequestRequest))]
[JsonSerializable(typeof(GitLabMergeRequestMergeRequest))]
[JsonSerializable(typeof(GitLabProjectResponse))]
[JsonSerializable(typeof(GitLabNamespaceResponse))]
[JsonSerializable(typeof(GitLabMergeRequestResponse))]
[JsonSerializable(typeof(GitLabUserResponse))]
[JsonSerializable(typeof(List<GitLabProjectResponse>))]
[JsonSerializable(typeof(List<GitLabMergeRequestResponse>))]
internal partial class GitLabJsonContext : JsonSerializerContext;
