namespace VcsManager.Tests;

public class FakeProviderConformanceTests : VcsProviderConformanceTests
{
    protected override IVcsProvider CreateProvider()
    {
        return new FakeVcsProvider();
    }

    protected override IVcsProvider CreateProviderWithMergeConflict()
    {
        return new FakeVcsProvider(mergeConflict: true);
    }

    protected override IVcsProvider CreateProviderWithUnsupportedMergeMethod()
    {
        return new FakeVcsProvider(unsupportedMergeMethod: UnsupportedMergeMethod);
    }
}
