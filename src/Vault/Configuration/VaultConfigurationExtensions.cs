using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vault.Abstractions;

namespace Vault.Configuration;

/// <summary>
/// Extensions to integrate HashiCorp Vault into the ASP.NET Core configuration system.
/// </summary>
public static class VaultConfigurationExtensions
{
    /// <summary>
    /// Adds HashiCorp Vault as a configuration source.
    /// Note: AddVault() must be called BEFORE this method to register VaultService.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="environment">The Vault environment to load (e.g., DEV, PROD).</param>
    /// <param name="configureSource">Optional action to configure the source.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddVaultConfiguration(
        this IConfigurationBuilder builder,
        string environment,
        Action<VaultConfigurationSource>? configureSource = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException(
                "Environment cannot be empty",
                nameof(environment));
        }

        var source = new VaultConfigurationSource
        {
            Environment = environment
        };

        configureSource?.Invoke(source);

        return builder.Add(source);
    }

    /// <summary>
    /// Adds HashiCorp Vault as a configuration source with an existing VaultService.
    /// Useful for tests or when VaultService is created manually.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="environment">The Vault environment to load (e.g., DEV, PROD).</param>
    /// <param name="vaultService">VaultService instance to use.</param>
    /// <param name="configureSource">Optional action to configure the source.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddVaultConfiguration(
        this IConfigurationBuilder builder,
        string environment,
        IVaultService vaultService,
        Action<VaultConfigurationSource>? configureSource = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException(
                "Environment cannot be empty",
                nameof(environment));
        }

        if (vaultService == null)
        {
            throw new ArgumentNullException(nameof(vaultService));
        }

        var source = new VaultConfigurationSource
        {
            Environment = environment
        };

        configureSource?.Invoke(source);

        var provider = new VaultConfigurationProvider(source, vaultService);
        builder.Add(new VaultConfigurationSourceWrapper(source, provider));

        return builder;
    }

    /// <summary>
    /// Inject VaultService into all existing VaultConfigurationProvider instances.
    /// To be called after IConfiguration is built to initialize the providers.
    /// </summary>
    /// <param name="configuration">The built configuration.</param>
    /// <param name="serviceProvider">The service provider containing VaultService.</param>
    public static void InitializeVaultProviders(
        this IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        if (configuration is not IConfigurationRoot configurationRoot)
        {
            return;
        }

        var vaultService = serviceProvider.GetService<IVaultService>();
        if (vaultService == null)
        {
            return;
        }

        var logger = serviceProvider.GetService<ILogger<VaultConfigurationProvider>>();

        foreach (var provider in configurationRoot.Providers)
        {
            if (provider is VaultConfigurationProvider vaultProvider)
            {
                vaultProvider.SetVaultService(vaultService, logger);
                vaultProvider.Load();
            }
        }
    }

    /// <summary>
    /// Wrapper to allow manual provider injection.
    /// </summary>
    private class VaultConfigurationSourceWrapper : IConfigurationSource
    {
        private readonly VaultConfigurationSource _source;
        private readonly VaultConfigurationProvider _provider;

        public VaultConfigurationSourceWrapper(
            VaultConfigurationSource source,
            VaultConfigurationProvider provider)
        {
            _source = source;
            _provider = provider;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return _provider;
        }
    }
}
