namespace Vault.Abstractions;

/// <summary>
/// Service for interacting with HashiCorp Vault.
/// </summary>
public interface IVaultService
{
    /// <summary>
    /// Lists all available environments in the Vault KV.
    /// </summary>
    /// <returns>The list of environment names.</returns>
    /// <exception cref="Exceptions.VaultException">When an error occurs during communication with Vault.</exception>
    Task<IEnumerable<string>> ListEnvironmentsAsync();

    /// <summary>
    /// Retrieves all secrets for a given environment.
    /// </summary>
    /// <param name="environment">The environment name (e.g., DEV, PROD).</param>
    /// <returns>A dictionary containing all secrets (key/value pairs).</returns>
    /// <exception cref="ArgumentException">If the environment is empty or null.</exception>
    /// <exception cref="Exceptions.VaultException">When an error occurs during communication with Vault.</exception>
    Task<Dictionary<string, object>> GetSecretsAsync(string environment);

    /// <summary>
    /// Retrieves a specific secret value.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <param name="key">The secret key.</param>
    /// <returns>The secret value, or null if the key does not exist.</returns>
    /// <exception cref="ArgumentException">If the environment or key is empty or null.</exception>
    /// <exception cref="Exceptions.VaultException">When an error occurs during communication with Vault.</exception>
    Task<object?> GetSecretValueAsync(string environment, string key);
}
