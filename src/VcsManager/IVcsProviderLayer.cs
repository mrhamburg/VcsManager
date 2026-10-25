namespace VcsManager;

/// <summary>
/// Decorates an existing provider with additional behavior.
/// </summary>
public interface IVcsProviderLayer
{
    /// <summary>
    /// Wraps the given provider.
    /// </summary>
    /// <param name="inner">The provider to wrap.</param>
    /// <returns>The wrapped provider.</returns>
    IVcsProvider Wrap(IVcsProvider inner);
}
