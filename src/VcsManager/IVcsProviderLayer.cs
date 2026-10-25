namespace VcsManager;

/// <summary>
/// Decorates an existing provider with additional behavior.
/// </summary>
/// <remarks>
/// <c>VcsProviderBuilder</c> applies layers in registration order, so the first
/// registered layer is the outermost wrapper.
/// </remarks>
public interface IVcsProviderLayer
{
    /// <summary>
    /// Wraps the given provider.
    /// </summary>
    /// <param name="inner">The provider to wrap.</param>
    /// <returns>The wrapped provider.</returns>
    IVcsProvider Wrap(IVcsProvider inner);
}
