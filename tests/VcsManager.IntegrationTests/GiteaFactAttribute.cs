using Xunit;
namespace VcsManager.IntegrationTests;

/// <summary>
/// Marks a Gitea integration test. The test is skipped when the live Gitea
/// configuration is absent.
/// </summary>
public sealed class GiteaFactAttribute : FactAttribute
{
    public GiteaFactAttribute()
    {
        if (!GiteaTestConfiguration.IsConfigured)
        {
            Skip = "Set VCS_GITEA_TEST_URL and VCS_GITEA_TEST_TOKEN to run the Gitea integration tests.";
        }
    }
}
