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
    /// Adds HashiCorp Vault as a configuration source with an existing VaultService.
    /// Secrets are loaded immediately during Build() so they are available for
    /// subsequent configuration such as Entity Framework connection strings.
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
            Environment = environment,
            VaultService = vaultService
        };

        configureSource?.Invoke(source);

        // The source will create the provider and load secrets immediately in Build()
        return builder.Add(source);
    }

    /// <summary>
    /// Adds HashiCorp Vault as a configuration source with an existing VaultService and logger.
    /// Secrets are loaded immediately during Build() so they are available for
    /// subsequent configuration such as Entity Framework connection strings.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="environment">The Vault environment to load (e.g., DEV, PROD).</param>
    /// <param name="vaultService">VaultService instance to use.</param>
    /// <param name="logger">Optional logger for the configuration provider.</param>
    /// <param name="configureSource">Optional action to configure the source.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddVaultConfiguration(
        this IConfigurationBuilder builder,
        string environment,
        IVaultService vaultService,
        ILogger<VaultConfigurationProvider>? logger,
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
            Environment = environment,
            VaultService = vaultService,
            Logger = logger
        };

        configureSource?.Invoke(source);

        // The source will create the provider and load secrets immediately in Build()
        return builder.Add(source);
    }
}
