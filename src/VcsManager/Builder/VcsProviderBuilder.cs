namespace VcsManager.Builder;

/// <summary>
/// Builds an <see cref="IVcsProvider"/> from fluent calls and configuration sources.
/// </summary>
public sealed class VcsProviderBuilder
{
    private readonly Dictionary<string, IVcsProviderFactory> _factories = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IVcsProviderLayer> _layers = [];
    private VcsProviderConfiguration? _configuration;

    /// <summary>
    /// Registers a provider factory so its name can be resolved at build time.
    /// </summary>
    /// <param name="factory">The factory to register.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
    public VcsProviderBuilder RegisterProvider(IVcsProviderFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factories[factory.ProviderName] = factory;
        return this;
    }

    /// <summary>
    /// Selects a provider by name and configures it with a settings dictionary.
    /// </summary>
    /// <param name="providerName">The provider name. Matching is case-insensitive.</param>
    /// <param name="configure">An optional action that adds settings.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentException"><paramref name="providerName"/> is empty or whitespace.</exception>
    public VcsProviderBuilder UseProvider(
        string providerName,
        Action<Dictionary<string, string>>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        _configuration = new VcsProviderConfiguration { ProviderName = providerName };
        configure?.Invoke(_configuration.Settings);
        return this;
    }

    /// <summary>
    /// Loads configuration from environment variables.
    /// </summary>
    /// <param name="prefix">The environment variable prefix.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="NotSupportedException">
    /// Environment variable reading is not available in this version.
    /// </exception>
    public VcsProviderBuilder FromEnvironment(string prefix = "VCS")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        throw new NotSupportedException(
            "Environment variable configuration is not available in this version. Configure the provider with UseProvider().");
    }

    /// <summary>
    /// Loads configuration from a YAML file.
    /// </summary>
    /// <param name="path">The YAML file path.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or whitespace.</exception>
    /// <exception cref="NotSupportedException">YAML reading is not available in this version.</exception>
    public VcsProviderBuilder FromYamlFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        throw new NotSupportedException(
            "YAML configuration is not available in this version. Configure the provider with UseProvider().");
    }

    /// <summary>
    /// Loads configuration from a YAML string.
    /// </summary>
    /// <param name="yaml">The YAML content.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentException"><paramref name="yaml"/> is empty or whitespace.</exception>
    /// <exception cref="NotSupportedException">YAML reading is not available in this version.</exception>
    public VcsProviderBuilder FromYaml(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);
        throw new NotSupportedException(
            "YAML configuration is not available in this version. Configure the provider with UseProvider().");
    }

    /// <summary>
    /// Adds a layer that wraps the provider.
    /// Layers are applied in registration order, so the first registered layer is outermost.
    /// </summary>
    /// <param name="layer">The layer to add.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="layer"/> is <see langword="null"/>.</exception>
    public VcsProviderBuilder AddLayer(IVcsProviderLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        _layers.Add(layer);
        return this;
    }

    /// <summary>
    /// Builds the configured provider.
    /// </summary>
    /// <returns>The provider, wrapped by the registered layers in order.</returns>
    /// <exception cref="InvalidOperationException">
    /// No provider is configured, or the provider name is not registered.
    /// </exception>
    public IVcsProvider Build()
    {
        if (_configuration is null || string.IsNullOrWhiteSpace(_configuration.ProviderName))
        {
            throw new InvalidOperationException(
                "No provider configured. Call UseProvider(), FromEnvironment(), FromYaml(), or FromYamlFile() before Build().");
        }

        if (!_factories.TryGetValue(_configuration.ProviderName, out var factory))
        {
            var registered = _factories.Count == 0
                ? "(none)"
                : string.Join(", ", _factories.Keys);

            throw new InvalidOperationException(
                $"No factory registered for provider '{_configuration.ProviderName}'. " +
                $"Registered providers: {registered}. " +
                "Ensure you have called the provider's registration extension before Build().");
        }

        var provider = factory.Create(_configuration.Settings);

        for (var i = _layers.Count - 1; i >= 0; i--)
        {
            provider = _layers[i].Wrap(provider);
        }

        return provider;
    }
}
