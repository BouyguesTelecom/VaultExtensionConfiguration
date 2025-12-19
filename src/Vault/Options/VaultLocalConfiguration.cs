namespace Vault.Options;

/// <summary>
/// Configuration pour l'authentification locale via token.
/// </summary>
public class VaultLocalConfiguration : VaultDefaultConfiguration
{
    /// <summary>
    /// Chemin du fichier token.
    /// Convention par défaut : %USERPROFILE%\.vault-token.
    /// Vous pouvez utiliser des variables d'environnement dans le chemin.
    /// </summary>
    public string TokenFilePath { get; set; } = "%USERPROFILE%\\.vault-token";
}
