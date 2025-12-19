using Vault.Abstractions;
using Vault.Exceptions;
using Vault.Internal;
using Vault.Options;
using Microsoft.Extensions.Logging;
using VaultSharp;
using VaultSharp.V1.SecretsEngines;

namespace Vault.Services;

/// <summary>
/// Implémentation du service Vault.
/// </summary>
public class VaultService : IVaultService
{
    private readonly IVaultClient _vaultClient;
    private readonly VaultOptions _options;
    private readonly ILogger<VaultService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultService"/> class.
    /// </summary>
    /// <param name="options">Options de configuration Vault.</param>
    /// <param name="logger">Logger pour les diagnostics.</param>
    /// <exception cref="ArgumentNullException">Si options ou logger est null.</exception>
    /// <exception cref="VaultConfigurationException">Si la configuration est invalide.</exception>
    /// <exception cref="VaultAuthenticationException">Si l'authentification échoue.</exception>
    public VaultService(
        VaultOptions options,
        ILogger<VaultService> logger)
    {
        this._options = options ?? throw new ArgumentNullException(nameof(options));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

        try
        {
            var authMethod = options.CreateAuthMethod();
            var config = options.GetConfiguration();

            var vaultClientSettings = new VaultClientSettings(config.VaultUrl, authMethod)
            {
                SecretsEngineMountPoints = new SecretsEngineMountPoints
                {
                    KeyValueV2 = config.MountPoint,
                },
            };

            // Configuration SSL
            if (config.IgnoreSslErrors)
            {
                _logger.LogWarning(
                    "Les erreurs SSL sont ignorées pour Vault. Cette configuration ne devrait PAS être utilisée en production.");

                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                var httpClient = new HttpClient(httpClientHandler);
                vaultClientSettings.MyHttpClientProviderFunc = handler => httpClient;
            }

            _vaultClient = new VaultClient(vaultClientSettings);

            _logger.LogInformation(
                "VaultService initialisé avec succès. Type d'authentification: {AuthType}, URL: {VaultUrl}",
                options.AuthenticationType,
                config.VaultUrl);
        }
        catch (VaultException)
        {
            // Re-throw les exceptions Vault custom
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'initialisation du VaultService");
            throw new VaultException("Erreur lors de l'initialisation du service Vault", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ListEnvironmentsAsync()
    {
        try
        {
            _logger.LogDebug("Récupération de la liste des environnements Vault");
            var listResponse = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(path: string.Empty);
            var environments = listResponse.Data.Keys.ToList();

            _logger.LogDebug("Liste des environnements récupérée: {Count} environnement(s)", environments.Count);
            return environments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de la liste des environnements");
            throw new VaultException("Erreur lors de la récupération de la liste des environnements", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object>> GetSecretsAsync(string environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("L'environnement ne peut pas être vide", nameof(environment));
        }

        try
        {
            _logger.LogDebug("Récupération des secrets pour l'environnement: {Environment}", environment);
            var secrets = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: environment);

            if (secrets?.Data?.Data == null)
            {
                _logger.LogDebug("Aucun secret trouvé pour l'environnement: {Environment}", environment);
                return new Dictionary<string, object>();
            }

            _logger.LogDebug(
                "Secrets récupérés pour l'environnement {Environment}: {Count} secret(s)",
                environment,
                secrets.Data.Data.Count);

            return new Dictionary<string, object>(secrets.Data.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des secrets pour l'environnement: {Environment}", environment);
            throw new VaultException($"Erreur lors de la récupération des secrets pour l'environnement '{environment}'", ex);
        }
    }

    /// <inheritdoc />
    public async Task<object?> GetSecretValueAsync(string environment, string key)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("L'environnement ne peut pas être vide", nameof(environment));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("La clé ne peut pas être vide", nameof(key));
        }

        try
        {
            _logger.LogDebug(
                "Récupération du secret {Key} pour l'environnement: {Environment}",
                key,
                environment);

            var secrets = await GetSecretsAsync(environment);
            secrets.TryGetValue(key, out var value);

            if (value == null)
            {
                _logger.LogDebug(
                    "Secret {Key} non trouvé dans l'environnement {Environment}",
                    key,
                    environment);
            }

            return value;
        }
        catch (ArgumentException)
        {
            // Re-throw les ArgumentException
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erreur lors de la récupération du secret {Key} pour l'environnement: {Environment}",
                key,
                environment);
            throw new VaultException(
                $"Erreur lors de la récupération du secret '{key}' pour l'environnement '{environment}'", ex);
        }
    }
}
