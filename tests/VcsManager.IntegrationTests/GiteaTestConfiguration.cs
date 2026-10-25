namespace VcsManager.IntegrationTests;

internal sealed record GiteaTestConfiguration(
    string BaseUrl,
    string Token,
    string? Organization)
{
    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VCS_GITEA_TEST_URL")) &&
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VCS_GITEA_TEST_TOKEN"));

    public static GiteaTestConfiguration FromEnvironment()
    {
        var baseUrl = Environment.GetEnvironmentVariable("VCS_GITEA_TEST_URL");
        var token = Environment.GetEnvironmentVariable("VCS_GITEA_TEST_TOKEN");

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Set VCS_GITEA_TEST_URL and VCS_GITEA_TEST_TOKEN to run the Gitea integration tests.");
        }

        var organization = Environment.GetEnvironmentVariable("VCS_GITEA_TEST_ORG");

        return new GiteaTestConfiguration(
            baseUrl.Trim(),
            token.Trim(),
            string.IsNullOrWhiteSpace(organization) ? null : organization.Trim());
    }
}
