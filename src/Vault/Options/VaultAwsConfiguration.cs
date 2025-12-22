namespace Vault.Options;

/// <summary>
/// Configuration pour l'authentification automatique via AWS IAM.
/// Utilise les credentials AWS disponibles (variables d'environnement, instance profile EC2, ECS task role, etc.).
/// </summary>
public class VaultAwsConfiguration
    : VaultDefaultConfiguration
{
    /// <summary>
    /// Nom du rôle Vault pour l'authentification AWS IAM (optionnel).
    /// Si non fourni, le rôle sera automatiquement déduit selon le pattern standard:
    /// {MountPoint}-{Environment}-role.
    /// Exemple: MountPoint="HELLOWORLD-FORMATION", Environment="thomas" -> "HELLOWORLD-FORMATION-thomas-role".
    /// Si vous souhaitez utiliser un nom de rôle différent, définissez explicitement cette propriété.
    /// </summary>
    public string? AwsIamRoleName { get; set; }

    /// <summary>
    /// Environnement de déploiement (dev, test, prod, thomas, etc.).
    /// Utilisé pour construire automatiquement le nom du rôle Vault si AwsIamRoleName n'est pas défini.
    /// Pattern: {MountPoint}-{Environment}-role.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Mount point de l'auth method AWS dans Vault.
    /// Par défaut: "aws".
    /// </summary>
    public string? AwsAuthMountPoint { get; set; } = "aws";
}
