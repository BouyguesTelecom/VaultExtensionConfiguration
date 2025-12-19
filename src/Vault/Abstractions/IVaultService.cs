namespace Vault.Abstractions;

/// <summary>
/// Service pour interagir avec HashiCorp Vault.
/// </summary>
public interface IVaultService
{
    /// <summary>
    /// Liste tous les environnements disponibles dans le KV Vault.
    /// </summary>
    /// <returns>La liste des noms d'environnements.</returns>
    /// <exception cref="Exceptions.VaultException">En cas d'erreur lors de la communication avec Vault.</exception>
    Task<IEnumerable<string>> ListEnvironmentsAsync();

    /// <summary>
    /// Récupère tous les secrets pour un environnement donné.
    /// </summary>
    /// <param name="environment">Le nom de l'environnement (ex: DEV, PROD).</param>
    /// <returns>Un dictionnaire contenant tous les secrets (clé/valeur).</returns>
    /// <exception cref="ArgumentException">Si l'environnement est vide ou null.</exception>
    /// <exception cref="Exceptions.VaultException">En cas d'erreur lors de la communication avec Vault.</exception>
    Task<Dictionary<string, object>> GetSecretsAsync(string environment);

    /// <summary>
    /// Récupère une valeur de secret spécifique.
    /// </summary>
    /// <param name="environment">Le nom de l'environnement.</param>
    /// <param name="key">La clé du secret.</param>
    /// <returns>La valeur du secret, ou null si la clé n'existe pas.</returns>
    /// <exception cref="ArgumentException">Si l'environnement ou la clé est vide ou null.</exception>
    /// <exception cref="Exceptions.VaultException">En cas d'erreur lors de la communication avec Vault.</exception>
    Task<object?> GetSecretValueAsync(string environment, string key);
}
