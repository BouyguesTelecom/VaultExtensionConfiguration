using Vault.Abstractions;
using Vault.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Vault;

/// <summary>
/// Extensions pour charger les secrets Vault dans la configuration de l'application.
/// </summary>
internal static class VaultConfigurationExtensions
{
    /// <summary>
    /// Charge les secrets depuis Vault et enrichit la configuration de l'application.
    /// Elle construit temporairement un ServiceProvider pour récupérer IVaultService,
    /// puis ajoute les secrets à builder.Configuration. Le vrai ServiceProvider
    /// sera créé lors de builder.Build() avec la configuration enrichie.
    /// </summary>
    /// <typeparam name="TBuilder">Type du builder (WebApplicationBuilder, HostApplicationBuilder, etc.).</typeparam>
    /// <param name="builder">Le IHostApplicationBuilder.</param>
    /// <param name="environment">Le nom de l'environnement Vault à charger.</param>
    /// <param name="sectionPrefix">Préfixe optionnel pour les clés dans la configuration.</param>
    /// <param name="addUnregisteredEntries">Si false, seules les clés existantes dans la configuration seront écrasées. Si true, toutes les entrées Vault seront ajoutées.</param>
    /// <returns>Le IHostApplicationBuilder pour permettre le chaînage.</returns>
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
    /// Charge les secrets depuis Vault et enrichit la configuration de l'application.
    /// Les secrets écrasent les valeurs existantes dans IConfiguration.
    /// </summary>
    /// <typeparam name="TBuilder">Type du builder (WebApplicationBuilder, HostApplicationBuilder, etc.).</typeparam>
    /// <param name="builder">Le IHostApplicationBuilder.</param>
    /// <param name="environment">Le nom de l'environnement Vault à charger (ex: "thomas", "dev", "prod").</param>
    /// <param name="sectionPrefix">Préfixe optionnel pour les clés dans la configuration (ex: "VaultSecrets"). Si null, les secrets écrasent directement les clés existantes.</param>
    /// <param name="addUnregisteredEntries">Si false, seules les clés existantes dans la configuration seront écrasées. Si true, toutes les entrées Vault seront ajoutées.</param>
    /// <returns>Le IHostApplicationBuilder pour permettre le chaînage.</returns>
    /// <exception cref="InvalidOperationException">Si IVaultService n'est pas enregistré.</exception>
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
