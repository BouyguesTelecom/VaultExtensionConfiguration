using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vault.Abstractions;
using Vault.Internal;

namespace Vault.Extensions;

/// <summary>
/// Provides extension methods for loading secrets from Vault and enriching the application's configuration during host
/// building.
/// </summary>
/// <remarks>These extension methods enable integration of Vault secrets into the configuration pipeline before
/// the host is built. They support both synchronous and asynchronous usage and allow customization of how secrets are
/// merged, including prefixing keys and controlling whether only existing configuration keys are overwritten. These
/// methods are intended for use with types implementing IHostApplicationBuilder, such as WebApplicationBuilder or
/// HostApplicationBuilder.</remarks>
internal static class VaultConfigurationExtensions
{
    /// <summary>
    /// Loads secrets from the vault for the specified environment and adds them to the application configuration
    /// synchronously.
    /// </summary>
    /// <remarks>This method blocks the calling thread until the secrets are loaded. For asynchronous loading,
    /// use LoadVaultSecretsAsync instead.</remarks>
    /// <typeparam name="TBuilder">The type of the host application builder.</typeparam>
    /// <param name="builder">The host application builder to which the vault secrets will be added. Cannot be null.</param>
    /// <param name="environment">The name of the environment for which to load secrets. Cannot be null or empty.</param>
    /// <param name="sectionPrefix">An optional prefix to apply to configuration section keys. If null, no prefix is applied.</param>
    /// <param name="addUnregisteredEntries">true to add secrets that are not registered in the configuration; otherwise, false. The default is true.</param>
    /// <returns>The host application builder with the vault secrets added to its configuration.</returns>
    internal static TBuilder LoadVaultSecrets<TBuilder>(
        this TBuilder builder,
        string environment,
        string? sectionPrefix = null,
        bool addUnregisteredEntries = true)
        where TBuilder : IHostApplicationBuilder
    {
        return LoadVaultSecretsAsync(builder, environment, sectionPrefix, addUnregisteredEntries)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Loads secrets from a vault for the specified environment and adds them to the application's configuration.
    /// </summary>
    /// <remarks>This method must be called before building the application host, as it modifies the
    /// configuration of the builder. Secrets are added with high priority and will override existing configuration
    /// values with the same keys.</remarks>
    /// <typeparam name="TBuilder">The type of the host application builder.</typeparam>
    /// <param name="builder">The host application builder to which the secrets will be added.</param>
    /// <param name="environment">The name of the environment for which to retrieve secrets. This value determines which set of secrets are loaded
    /// from the vault.</param>
    /// <param name="sectionPrefix">An optional prefix to apply to each configuration key. If specified, all secret keys will be prefixed
    /// accordingly. Can be null.</param>
    /// <param name="addUnregisteredEntries">true to add all secrets from the vault to the configuration, including those not already present; false to add
    /// only secrets that match existing configuration keys. The default is true.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the original builder instance with
    /// the secrets added to its configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the IVaultService is not registered in the service collection.</exception>
    internal static async Task<TBuilder> LoadVaultSecretsAsync<TBuilder>(
        this TBuilder builder,
        string environment,
        string? sectionPrefix = null,
        bool addUnregisteredEntries = true)
        where TBuilder : IHostApplicationBuilder
    {
        // Construire temporairement le service provider pour obtenir IVaultService
        // Ce ServiceProvider est différent de celui qui sera créé par builder.Build()
        // C'est un pattern nécessaire car on doit modifier builder.Configuration AVANT Build()
        using var serviceProvider = builder.Services.BuildServiceProvider();

        var vaultService = serviceProvider.GetService<IVaultService>()
            ?? throw new InvalidOperationException("IVaultService n'est pas enregistré.");

        // Récupérer tous les secrets de l'environnement spécifié
        var secrets = await vaultService.GetSecretsAsync(environment);

        // Aplatir le dictionnaire pour supporter les structures imbriquées
        var flattenedSecrets = ConfigurationFlattener.FlattenDictionary(secrets, sectionPrefix);

        // Si addUnregisteredEntries est false, filtrer pour ne garder que les clés existantes
        if (!addUnregisteredEntries)
        {
            flattenedSecrets = flattenedSecrets
                .Where(kvp => builder.Configuration[kvp.Key] != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        // Ajouter les secrets à la configuration
        // AddInMemoryCollection ajoute les valeurs avec une priorité plus élevée,
        // donc elles écraseront les valeurs existantes
        builder.Configuration.AddInMemoryCollection(flattenedSecrets);

        return builder;
    }
}
