namespace Vault.Abstractions;

/// <summary>
/// Defines methods for retrieving environments and secrets from a key-value vault service.
/// </summary>
/// <remarks>Implementations of this interface provide asynchronous access to secrets organized by environment.
/// Methods support retrieving all environments, all secrets for a specific environment, individual secret values, and
/// nested secret values using dot notation. This interface is intended for use in applications that require secure,
/// environment-based secret management.</remarks>
public interface IVaultService
{
    /// <summary>
    /// Lists all available environments in the KV Vault.
    /// </summary>
    Task<IEnumerable<string>> ListEnvironmentsAsync();

    /// <summary>
    /// Retrieves all secrets for a given environment.
    /// </summary>
    /// <param name="environment">The environment name (e.g., DEV, PROD).</param>
    Task<Dictionary<string, object>> GetSecretsAsync(string environment);

    /// <summary>
    /// Retrieves a specific secret value.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <param name="key">The secret key.</param>
    Task<object?> GetSecretValueAsync(string environment, string key);

    /// <summary>
    /// Retrieves a nested secret value using dot notation.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <param name="path">The secret path with dot notation (e.g., "level1.level2.level3").</param>
    /// <returns>The secret value if found, otherwise null.</returns>
    /// <example>
    /// await vaultService.GetNestedSecretValueAsync("thomas", "level1.level2.level3").
    /// </example>
    Task<object?> GetNestedSecretValueAsync(string environment, string path);
}
