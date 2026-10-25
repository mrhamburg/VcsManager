using VcsManager.Internal;

namespace VcsManager.Tests;

public class SeedRequestValidatorTests
{

    [Fact]
    public void Normalize_RejectsNullRequest()
    {
        Assert.Throws<ArgumentNullException>(() => SeedRequestValidator.Normalize(null!));
    }

    [Fact]
    public void Normalize_TrimsBranchAndDefaultsCommitMessage()
    {
        var request = new SeedRepositoryRequest
        {
            Branch = "  main  ",
            Files = [File("README.md", "hello")],
        };

        var normalized = SeedRequestValidator.Normalize(request);

        Assert.Equal("main", normalized.Branch);
        Assert.Equal("Initial commit", normalized.CommitMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_RejectsMissingBranch(string? branch)
    {
        var request = new SeedRepositoryRequest
        {
            Branch = branch!,
            Files = [File("README.md", "hello")],
        };

        Assert.Throws<ArgumentException>(() => SeedRequestValidator.Normalize(request));
    }


    [Fact]
    public void Normalize_PreservesCustomCommitMessage()
    {
        var request = new SeedRepositoryRequest
        {
            Branch = "main",
            CommitMessage = "  Custom seed message  ",
            Files = [File("README.md", "hello")],
        };

        var normalized = SeedRequestValidator.Normalize(request);

        Assert.Equal("Custom seed message", normalized.CommitMessage);
    }

    [Fact]
    public void Normalize_RejectsMissingFiles()
    {
        var request = new SeedRepositoryRequest
        {
            Branch = "main",
            Files = [],
        };

        Assert.Throws<ArgumentException>(() => SeedRequestValidator.Normalize(request));
    }

    [Theory]
    [InlineData("/absolute.txt")]
    [InlineData("../outside.txt")]
    [InlineData("nested/../outside.txt")]
    [InlineData("./inside.txt")]
    [InlineData("nested//empty.txt")]
    [InlineData("nested\\windows.txt")]
    [InlineData("nested/.git/config")]
    [InlineData(".git/config")]
    public void Normalize_RejectsInvalidPaths(string path)
    {
        var request = new SeedRepositoryRequest
        {
            Branch = "main",
            Files = [File(path, "content")],
        };

        Assert.Throws<ArgumentException>(() => SeedRequestValidator.Normalize(request));
    }

    [Fact]
    public void Normalize_RejectsDuplicatePaths()
    {
        var request = new SeedRepositoryRequest
        {
            Branch = "main",
            Files =
            [
                File("README.md", "one"),
                File("README.md", "two"),
            ],
        };

        Assert.Throws<ArgumentException>(() => SeedRequestValidator.Normalize(request));
    }

    [Fact]
    public void Normalize_RejectsNullFile()
    {
        var request = new SeedRepositoryRequest
        {
            Branch = "main",
            Files = [null!],
        };

        Assert.Throws<ArgumentNullException>(() => SeedRequestValidator.Normalize(request));
    }

    [Fact]
    public void Normalize_PreservesBinaryContent()
    {
        var content = new byte[] { 0, 1, 2, 255 };
        var request = new SeedRepositoryRequest
        {
            Branch = "main",
            Files = [new RepositorySeedFile { Path = "binary.bin", Content = content }],
        };

        var normalized = SeedRequestValidator.Normalize(request);

        Assert.Equal(content, normalized.Files[0].Content.ToArray());
    }

    private static RepositorySeedFile File(string path, string content)
    {
        return new RepositorySeedFile
        {
            Path = path,
            Content = System.Text.Encoding.UTF8.GetBytes(content),
        };
    }
}
