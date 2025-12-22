namespace Vault.Options;

/// <summary>
/// Configuration de base pour l'accès à Vault.
/// Contient les paramètres communs à tous les types d'authentification.
/// </summary>
public class VaultDefaultConfiguration
{
    /// <summary>
    /// URL du serveur Vault.
    /// </summary>
    public string VaultUrl { get; set; } = string.Empty;

    /// <summary>
    /// Mount point du KV v2 dans Vault.
    /// </summary>
    public string MountPoint { get; set; } = string.Empty;

    /// <summary>
    /// Indique si les erreurs SSL doivent être ignorées.
    /// À désactiver en production pour des raisons de sécurité.
    /// </summary>
    public bool IgnoreSslErrors { get; set; } = true;
}
