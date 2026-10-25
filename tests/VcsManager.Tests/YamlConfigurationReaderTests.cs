using VcsManager.Builder;

namespace VcsManager.Tests;

public class YamlConfigurationReaderTests
{
    [Fact]
    public void Read_ValidYaml_ReturnsConfiguration()
    {
        var yaml = """
            provider:
              github:
                token: abc
                base:
                  url: https://github.example.com
            """;

        var configuration = YamlConfigurationReader.Read(yaml);

        Assert.Equal("github", configuration.ProviderName);
        Assert.Equal("abc", configuration.Settings["token"]);
        Assert.Equal("https://github.example.com", configuration.Settings["base.url"]);
    }

    [Fact]
    public void Read_NestedSettings_AreFlattenedWithDots()
    {
        var yaml = """
            provider:
              gitlab:
                server: https://gitlab.example.com
                auth:
                  token:
                    secret: my-token
            """;

        var configuration = YamlConfigurationReader.Read(yaml);

        Assert.Equal("gitlab", configuration.ProviderName);
        Assert.Equal("https://gitlab.example.com", configuration.Settings["server"]);
        Assert.Equal("my-token", configuration.Settings["auth.token.secret"]);
    }

    [Fact]
    public void Read_MissingProviderKey_Throws()
    {
        var yaml = """
            notprovider:
              github:
                token: abc
            """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => YamlConfigurationReader.Read(yaml));

        Assert.Contains("provider", exception.Message);
    }

    [Fact]
    public void Read_ZeroProviders_Throws()
    {
        var yaml = """
            provider: {}
            """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => YamlConfigurationReader.Read(yaml));

        Assert.Contains("Exactly one provider", exception.Message);
        Assert.Contains("Found 0", exception.Message);
    }

    [Fact]
    public void Read_MultipleProviders_Throws()
    {
        var yaml = """
            provider:
              github:
                token: abc
              gitlab:
                token: def
            """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => YamlConfigurationReader.Read(yaml));

        Assert.Contains("Exactly one provider", exception.Message);
        Assert.Contains("Found 2", exception.Message);
    }

    [Fact]
    public void Read_EmptyYaml_Throws()
    {
        Assert.Throws<ArgumentException>(() => YamlConfigurationReader.Read(""));
    }

    [Fact]
    public void Read_ProviderWithNoSettings_ReturnsEmptySettings()
    {
        var yaml = """
            provider:
              github:
            """;

        var configuration = YamlConfigurationReader.Read(yaml);

        Assert.Equal("github", configuration.ProviderName);
        Assert.Empty(configuration.Settings);
    }
}
