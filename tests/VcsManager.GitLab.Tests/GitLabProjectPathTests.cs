using VcsManager.GitLab.Internal;

namespace VcsManager.GitLab.Tests;

public sealed class GitLabProjectPathTests
{
    [Fact]
    public void From_WithOwnerAndName_CombinesPath()
    {
        var repository = new RepositoryRef { Owner = "group", Name = "project" };

        var path = GitLabProjectPath.From(repository, defaultNamespace: null);

        Assert.Equal("group/project", path);
    }

    [Fact]
    public void From_WithNestedSubgroup_CombinesAllSegments()
    {
        var repository = new RepositoryRef
        {
            Owner = "group",
            Project = "subgroup/team",
            Name = "project",
        };

        var path = GitLabProjectPath.From(repository, defaultNamespace: null);

        Assert.Equal("group/subgroup/team/project", path);
    }

    [Fact]
    public void From_WithoutOwner_UsesDefaultNamespace()
    {
        var repository = new RepositoryRef { Name = "project" };

        var path = GitLabProjectPath.From(repository, "default-group");

        Assert.Equal("default-group/project", path);
    }

    [Fact]
    public void From_WithoutAnyNamespace_ThrowsArgumentException()
    {
        var repository = new RepositoryRef { Name = "project" };

        Assert.Throws<ArgumentException>(
            () => GitLabProjectPath.From(repository, defaultNamespace: null));
    }

    [Fact]
    public void Encode_EscapesNestedNamespaceSeparators()
    {
        var encoded = GitLabProjectPath.Encode("group/subgroup/project");

        Assert.Equal("group%2Fsubgroup%2Fproject", encoded);
    }
}
