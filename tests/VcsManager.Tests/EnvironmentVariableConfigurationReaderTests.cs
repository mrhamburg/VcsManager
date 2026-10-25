using VcsManager.Builder;

namespace VcsManager.Tests;

public class EnvironmentVariableConfigurationReaderTests
{
    [Fact]
    public void Read_CompleteConfiguration_ReturnsProviderAndFlattenedSettings()
    {
        const string prefix = "VCSM_TEST_ENV_COMPLETE";
        Set(prefix, "PROVIDER", "testprovider");
        Set(prefix, "TESTPROVIDER_TOKEN", "abc");
        Set(prefix, "TESTPROVIDER_BASE_URL", "https://example.test");

        try
        {
            var configuration = EnvironmentVariableConfigurationReader.Read(prefix);

            Assert.Equal("testprovider", configuration.ProviderName);
            Assert.Equal("abc", configuration.Settings["token"]);
            Assert.Equal("https://example.test", configuration.Settings["base.url"]);
        }
        finally
        {
            Clear(prefix, "PROVIDER", "TESTPROVIDER_TOKEN", "TESTPROVIDER_BASE_URL");
        }
    }

    [Fact]
    public void Read_IgnoresWhitespaceAndMissingValues()
    {
        const string prefix = "VCSM_TEST_ENV_WHITESPACE";
        Set(prefix, "PROVIDER", "testprovider");
        Set(prefix, "TESTPROVIDER_TOKEN", "   ");
        Set(prefix, "TESTPROVIDER_EMPTY", "");

        try
        {
            var configuration = EnvironmentVariableConfigurationReader.Read(prefix);

            Assert.Empty(configuration.Settings);
        }
        finally
        {
            Clear(prefix, "PROVIDER", "TESTPROVIDER_TOKEN", "TESTPROVIDER_EMPTY");
        }
    }

    [Fact]
    public void Read_MissingProvider_ThrowsInvalidOperationException()
    {
        const string prefix = "VCSM_TEST_ENV_MISSING";

        var exception = Assert.Throws<InvalidOperationException>(
            () => EnvironmentVariableConfigurationReader.Read(prefix));

        Assert.Contains($"{prefix}_PROVIDER", exception.Message);
    }

    [Fact]
    public void Read_ProviderNameIsCaseInsensitiveForSettingsPrefix()
    {
        const string prefix = "VCSM_TEST_ENV_CASE";
        Set(prefix, "PROVIDER", "TestProvider");
        Set(prefix, "TESTPROVIDER_TOKEN", "abc");

        try
        {
            var configuration = EnvironmentVariableConfigurationReader.Read(prefix);

            Assert.Equal("TestProvider", configuration.ProviderName);
            Assert.Equal("abc", configuration.Settings["token"]);
        }
        finally
        {
            Clear(prefix, "PROVIDER", "TESTPROVIDER_TOKEN");
        }
    }

    [Fact]
    public void Read_TrimsValues()
    {
        const string prefix = "VCSM_TEST_ENV_TRIM";
        Set(prefix, "PROVIDER", "testprovider");
        Set(prefix, "TESTPROVIDER_TOKEN", "abc\n");

        try
        {
            var configuration = EnvironmentVariableConfigurationReader.Read(prefix);

            Assert.Equal("abc", configuration.Settings["token"]);
        }
        finally
        {
            Clear(prefix, "PROVIDER", "TESTPROVIDER_TOKEN");
        }
    }

    private static void Set(string prefix, string name, string value)
    {
        Environment.SetEnvironmentVariable($"{prefix}_{name}", value);
    }

    private static void Clear(string prefix, params string[] names)
    {
        foreach (var name in names)
        {
            Environment.SetEnvironmentVariable($"{prefix}_{name}", null);
        }
    }
}
