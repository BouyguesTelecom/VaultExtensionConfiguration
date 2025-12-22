using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vault.Abstractions;
using Vault.Exceptions;
using Vault.Options;
using Vault.Services;
using Vault.Validators;

namespace Vault.Extensions;

/// <summary>
/// Provides extension methods for registering Vault services and configuration with an application's dependency
/// injection container and configuration pipeline.
/// </summary>
/// <remarks>These extension methods enable integration of Vault for secret management and configuration loading
/// during application startup. Use these methods to add Vault support to your application's service collection or host
/// builder. The class is intended to be used as part of the application's initialization process.</remarks>
public static class VaultServiceCollectionExtensions
{
    /// <summary>
    /// Adds Vault configuration and secret loading to the application builder.
    /// </summary>
    /// <remarks>This method registers Vault services and, if an environment is specified, loads secrets from Vault
    /// into the application's configuration. It is intended to be used during application startup as part of the
    /// configuration pipeline.</remarks>
    /// <typeparam name="TBuilder">The type of the host application builder.</typeparam>
    /// <param name="builder">The application builder to which Vault services and configuration will be added.</param>
    /// <param name="configureOptions">A delegate to configure Vault options before adding the Vault service.</param>
    /// <param name="environment">The name of the environment for which to load Vault secrets. If null or whitespace, secrets are not loaded.</param>
    /// <param name="sectionPrefix">An optional prefix to apply to configuration section keys when loading secrets. Can be null.</param>
    /// <param name="addUnregisteredEntries">true to include Vault secrets that are not registered in the configuration; otherwise, false.</param>
    /// <returns>The application builder instance, to allow for method chaining.</returns>
    public static TBuilder AddVault<TBuilder>(
        this TBuilder builder,
        Action<VaultOptions> configureOptions,
        string environment,
        string? sectionPrefix = null,
        bool addUnregisteredEntries = false)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddVaultService(configureOptions);

        if (!string.IsNullOrWhiteSpace(environment))
        {
            builder.LoadVaultSecrets(environment, sectionPrefix, addUnregisteredEntries);
        }

        return builder;
    }

    /// <summary>
    /// Adds the Vault service and its configuration to the specified service collection.
    /// </summary>
    /// <remarks>This method registers the Vault options and the Vault service as singletons. It validates the
    /// provided options before registration. Call this method during application startup to enable Vault
    /// integration.</remarks>
    /// <param name="services">The service collection to which the Vault service and configuration will be added.</param>
    /// <param name="configureOptions">A delegate that configures the Vault options. Used to set required parameters for the Vault service.</param>
    /// <returns>The same instance of <see cref="IServiceCollection"/> that was provided, to support method chaining.</returns>
    /// <exception cref="VaultConfigurationException">Thrown if the configured Vault options are invalid.</exception>
    public static IServiceCollection AddVaultService(this IServiceCollection services, Action<VaultOptions> configureOptions)
    {
        // Créer et configurer les options
        var vaultOptions = new VaultOptions();
        configureOptions(vaultOptions);

        // Valider les options avec FluentValidation
        var validator = new VaultOptionsValidator();
        var validationResult = validator.Validate(vaultOptions);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(Environment.NewLine, validationResult.Errors.Select(e => e.ErrorMessage));
            throw new VaultConfigurationException(errors);
        }

        // Enregistrer VaultOptions et VaultService comme singletons
        services.AddSingleton(vaultOptions);
        services.AddSingleton<IVaultService, VaultService>();

        return services;
    }
}
