using Vault.Abstractions;
using Vault.Exceptions;
using Vault.Options;
using Vault.Services;
using Vault.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Vault;

/// <summary>
/// Extensions pour l'enregistrement du service Vault dans la collection de services.
/// </summary>
public static class VaultServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute le service Vault et charge automatiquement les secrets dans la configuration.
    /// ? API SIMPLIFIÉE : Cette méthode fait tout en un seul appel.
    /// Compatible avec WebApplicationBuilder (ASP.NET Core), HostApplicationBuilder (console, worker), etc.
    /// </summary>
    /// <typeparam name="TBuilder">Type du builder (WebApplicationBuilder, HostApplicationBuilder, etc.).</typeparam>
    /// <param name="builder">Le IHostApplicationBuilder.</param>
    /// <param name="configureOptions">Action pour configurer les options Vault.</param>
    /// <param name="environment">Le nom de l'environnement Vault à charger (ex: "production", "dev", "thomas").</param>
    /// <param name="sectionPrefix">Préfixe optionnel pour les clés dans la configuration. Si null, les secrets écrasent directement les clés existantes.</param>
    /// <param name="addUnregisteredEntries">Si false, seules les clés existantes dans la configuration seront écrasées. Si true, toutes les entrées Vault seront ajoutées.</param>
    /// <returns>Le IHostApplicationBuilder pour permettre le chaînage.</returns>
    /// <exception cref="VaultConfigurationException">Si la configuration Vault est invalide.</exception>
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
    /// Ajoute le service Vault à la collection de services SANS charger les secrets.
    /// ?? Pour la plupart des cas, utilisez plutôt AddVault(builder, ...) qui charge automatiquement les secrets.
    /// </summary>
    /// <param name="services">La collection de services.</param>
    /// <param name="configureOptions">Action pour configurer les options Vault.</param>
    /// <returns>La collection de services pour permettre le chaînage.</returns>
    /// <exception cref="VaultConfigurationException">Si la configuration Vault est invalide.</exception>
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
