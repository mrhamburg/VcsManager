using VcsManager.Builder;

namespace VcsManager.Samples;

internal sealed class DemoVcsProviderFactory : IVcsProviderFactory
{
    public string ProviderName => "demo";

    public IVcsProvider Create(IReadOnlyDictionary<string, string> configuration)
    {
        return new DemoVcsProvider();
    }
}
