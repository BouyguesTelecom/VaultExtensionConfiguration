using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        // Register VaultOptions singleton
        services.AddSingleton(vaultOptions);

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
}
