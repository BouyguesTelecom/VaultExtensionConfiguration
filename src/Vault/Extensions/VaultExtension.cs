using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vault.Abstractions;
using Vault.Configuration;
using Vault.Options;
using Vault.Services;
using Vault.Validators;

namespace Vault.Extentions;

/// <summary>
/// Extensions to configure HashiCorp Vault in dependency injection.
/// </summary>
public static class VaultExtension
{
    /// <summary>
    /// Fully configures Vault: adds configuration source and registers VaultService.
    /// Secrets are loaded immediately during configuration build, making them available
    /// for subsequent configuration such as Entity Framework connection strings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="vaultOptions">The Vault options read from appsettings.</param>
    /// <param name="environment">The Vault environment to load (e.g., DEV, PROD, thomas).</param>
    /// <param name="configureSource">Optional action to configure the Vault configuration source.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVault(
        this IServiceCollection services,
        IConfigurationBuilder configuration,
        VaultOptions vaultOptions,
        string environment,
        Action<VaultConfigurationSource>? configureSource = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (vaultOptions == null)
        {
            throw new ArgumentNullException(nameof(vaultOptions));
        }

        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("Environment cannot be empty", nameof(environment));
        }

        // Register VaultOptions singleton - directly for backward compatibility
        services.AddSingleton(vaultOptions);

        // Create a custom options factory that returns the singleton instance
        var factory = new CustomVaultOptionsFactory(vaultOptions);

        // Register IOptionsFactory<VaultOptions>
        services.AddSingleton<IOptionsFactory<VaultOptions>>(factory);

        // Register IOptions<VaultOptions>
        services.AddSingleton<IOptions<VaultOptions>>(sp =>
            new OptionsWrapper<VaultOptions>(sp.GetRequiredService<VaultOptions>()));

        // Register IOptionsSnapshot<VaultOptions>
        services.AddScoped<IOptionsSnapshot<VaultOptions>>(sp =>
            new CustomVaultOptionsSnapshot(sp.GetRequiredService<VaultOptions>()));

        // Register IOptionsMonitor<VaultOptions>
        services.AddSingleton<IOptionsMonitor<VaultOptions>>(sp =>
            new CustomVaultOptionsMonitor(sp.GetRequiredService<IOptionsFactory<VaultOptions>>()));

        // If Vault is not activated, do nothing more
        if (!vaultOptions.IsActivated)
        {
            return services;
        }

        // Validate VaultOptions configuration
        VaultOptionsValidator.Validate(vaultOptions);

        // Create VaultService immediately so secrets can be loaded during configuration build
        var vaultService = new VaultService(vaultOptions);

        // Add Vault configuration with immediate loading
        configuration.AddVaultConfiguration(environment, vaultService, configureSource);

        // Register the same VaultService instance in DI for later use
        services.AddSingleton<IVaultService>(vaultService);

        return services;
    }

    /// <summary>
    /// Custom factory for VaultOptions that returns a fixed singleton instance.
    /// Enables support for IOptions, IOptionsSnapshot, and IOptionsMonitor patterns.
    /// </summary>
    private sealed class CustomVaultOptionsFactory : IOptionsFactory<VaultOptions>
    {
        private readonly VaultOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomVaultOptionsFactory"/> class.
        /// </summary>
        /// <param name="options">The VaultOptions instance to return.</param>
        public CustomVaultOptionsFactory(VaultOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Create an instance of TOptions with the name.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <returns>The VaultOptions instance.</returns>
        public VaultOptions Create(string name)
        {
            return _options;
        }
    }

    /// <summary>
    /// Custom snapshot for VaultOptions that wraps the singleton instance.
    /// </summary>
    private sealed class CustomVaultOptionsSnapshot : IOptionsSnapshot<VaultOptions>
    {
        private readonly VaultOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomVaultOptionsSnapshot"/> class.
        /// </summary>
        /// <param name="options">The VaultOptions instance.</param>
        public CustomVaultOptionsSnapshot(VaultOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets the default options instance.
        /// </summary>
        public VaultOptions Value => _options;

        /// <summary>
        /// Gets a named options instance.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <returns>The options instance.</returns>
        public VaultOptions Get(string? name)
        {
            return _options;
        }
    }

    /// <summary>
    /// Custom monitor for VaultOptions that observes changes to a fixed singleton instance.
    /// </summary>
    private sealed class CustomVaultOptionsMonitor : IOptionsMonitor<VaultOptions>
    {
        private readonly IOptionsFactory<VaultOptions> _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomVaultOptionsMonitor"/> class.
        /// </summary>
        /// <param name="factory">The options factory.</param>
        public CustomVaultOptionsMonitor(IOptionsFactory<VaultOptions> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Gets the current value of the options.
        /// </summary>
        public VaultOptions CurrentValue => _factory.Create("DEFAULT");

        /// <summary>
        /// Gets a named options instance.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <returns>The options instance.</returns>
        public VaultOptions Get(string? name)
        {
            return _factory.Create(name ?? "DEFAULT");
        }

        /// <summary>
        /// Registers a listener to be called whenever options are changed.
        /// </summary>
        /// <param name="listener">The listener function.</param>
        /// <returns>A disposable that can be used to unregister the listener.</returns>
        public IDisposable OnChange(Action<VaultOptions, string> listener)
        {
            // Since VaultOptions is static, we don't need to do anything on change
            return new NoOpDisposable();
        }

        /// <summary>
        /// No-op disposable for the change listener.
        /// </summary>
        private sealed class NoOpDisposable : IDisposable
        {
            /// <summary>
            /// Dispose (no-op).
            /// </summary>
            public void Dispose()
            {
            }
        }
    }
}
